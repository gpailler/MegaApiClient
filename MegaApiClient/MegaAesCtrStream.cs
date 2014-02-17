#region License

/*
The MIT License (MIT)

Copyright (c) 2014 Gregoire Pailler

Permission is hereby granted, free of charge, to any person obtaining a copy of
this software and associated documentation files (the "Software"), to deal in
the Software without restriction, including without limitation the rights to
use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
the Software, and to permit persons to whom the Software is furnished to do so,
subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CG.Web.MegaApiClient
{
    internal class MegaAesCtrStreamCrypter : MegaAesCtrStream
    {
        public MegaAesCtrStreamCrypter(Stream stream)
            : base(stream, stream.Length, Mode.Crypt, Crypto.CreateAesKey(), Crypto.CreateAesKey().CopySubArray(8))
        {
        }

        public byte[] FileKey
        {
            get { return this._fileKey; }
        }

        public byte[] Iv
        {
            get { return this._iv; }
        }

        public byte[] MetaMac
        {
            get
            {
                if (this._position != this._streamLength)
                {
                    throw new NotSupportedException("Stream must be fully read to obtain computed FileMac");
                }

                return this._metaMac;
            }
        }
    }

    internal class MegaAesCtrStreamDecrypter : MegaAesCtrStream
    {
        private readonly byte[] _expectedMetaMac;

        public MegaAesCtrStreamDecrypter(Stream stream, long streamLength, byte[] fileKey, byte[] iv, byte[] expectedMetaMac)
            : base(stream, streamLength, Mode.Decrypt, fileKey, iv)
        {
            if (expectedMetaMac == null || expectedMetaMac.Length != 8)
            {
                throw new ArgumentException("Invalid expectedMetaMac");
            }

            this._expectedMetaMac = expectedMetaMac;
        }

        protected override void OnStreamRead()
        {
            if (!this._expectedMetaMac.SequenceEqual(this._metaMac))
            {
                throw new DownloadException();
            }
        }
    }

    internal abstract class MegaAesCtrStream : Stream
    {
        protected readonly byte[] _fileKey;
        protected readonly byte[] _iv;
        protected readonly long _streamLength;
        protected long _position = 0;
        protected byte[] _metaMac = new byte[8];

        private readonly Stream _stream;
        private readonly Mode _mode;
        private readonly long[] _chunksPositions;
        private readonly byte[] _counter = new byte[8];
        private long _currentCounter = 0;
        private byte[] _currentChunkMac = new byte[16];
        private byte[] _fileMac = new byte[16];

        protected MegaAesCtrStream(Stream stream, long streamLength, Mode mode, byte[] fileKey, byte[] iv)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            if (fileKey == null || fileKey.Length != 16)
            {
                throw new ArgumentException("Invalid fileKey");
            }

            if (iv == null || iv.Length != 8)
            {
                throw new ArgumentException("Invalid Iv");
            }

            this._stream = stream;
            this._streamLength = streamLength;
            this._mode = mode;
            this._fileKey = fileKey;
            this._iv = iv;

            this._chunksPositions = GetChunksPositions(this._streamLength);
        }
        
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (this._position == this._streamLength)
            {
                return 0;
            }

            for (long pos = this._position; pos < Math.Min(this._position + count, this._streamLength); pos += 16)
            {
                // We are on a chunk bondary
                if (this._chunksPositions.Any(chunk => chunk == pos) || pos == 0)
                {
                    if (pos != 0)
                    {
                        // Compute the current chunk mac data on each chunk bondary
                        this.ComputeChunk();
                    }

                    // Init chunk mac with Iv values
                    for (int i = 0; i < 8; i++)
                    {
                        _currentChunkMac[i] = this._iv[i];
                        _currentChunkMac[i + 8] = this._iv[i];
                    }
                }

                this.IncrementCounter();

                // Iterate each AES 16 bytes block
                byte[] input = new byte[16];
                byte[] output = new byte[input.Length];
                int inputLength = this._stream.Read(input, 0, input.Length);
                if (inputLength != input.Length)
                {
                    // Sometimes, the stream is not finished but the read is not complete
                    inputLength += this._stream.Read(input, inputLength, input.Length - inputLength);
                }

                // Merge Iv and counter
                byte[] ivCounter = new byte[16];
                Array.Copy(this._iv, ivCounter, 8);
                Array.Copy(this._counter, 0, ivCounter, 8, 8);

                byte[] encryptedIvCounter = Crypto.EncryptAes(ivCounter, this._fileKey);

                for (int inputPos = 0; inputPos < inputLength; inputPos++)
                {
                    output[inputPos] = (byte)(encryptedIvCounter[inputPos] ^ input[inputPos]);
                    this._currentChunkMac[inputPos] ^= (_mode == Mode.Crypt) ? input[inputPos] : output[inputPos];
                }

                // Copy to buffer
                Array.Copy(output, 0, buffer, offset + pos - this._position, Math.Min(output.Length, this._streamLength - pos));

                // Crypt to current chunk mac
                this._currentChunkMac = Crypto.EncryptAes(this._currentChunkMac, this._fileKey);
            }

            long len = (long)Math.Min(count, this._streamLength - this._position);
            this._position += len;

            // When stream is fully processed, we compute the last chunk
            if (this._position == this._streamLength)
            {
                this.ComputeChunk();

                // Compute Meta MAC
                for (int i = 0; i < 4; i++)
                {
                    this._metaMac[i] = (byte)(this._fileMac[i] ^ this._fileMac[i + 4]);
                    this._metaMac[i + 4] = (byte)(this._fileMac[i + 8] ^ this._fileMac[i + 12]);
                }

                this.OnStreamRead();
            }

            return (int)len;
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override long Length
        {
            get { return this._streamLength; }
        }

        public override long Position
        {
            get { return this._position; }
            set
            {
                if (this._position != value)
                {
                    throw new NotSupportedException("Seek is not supported");
                }
            }
        }

        protected enum Mode
        {
            Crypt,
            Decrypt
        }

        protected virtual void OnStreamRead()
        {
        }

        private void IncrementCounter()
        {
            byte[] counter = BitConverter.GetBytes(_currentCounter++);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(counter);
            }
            Array.Copy(counter, this._counter, 8);
        }

        private void ComputeChunk()
        {
            for (int i = 0; i < 16; i++)
            {
                this._fileMac[i] ^= this._currentChunkMac[i];
            }

            this._fileMac = Crypto.EncryptAes(this._fileMac, this._fileKey);
        }

        private long[] GetChunksPositions(long size)
        {
            List<long> chunks = new List<long>();

            long chunkStartPosition = 0;
            for (int idx = 1; (idx <= 8) && (chunkStartPosition < (size - (idx * 131072))); idx++)
            {
                chunkStartPosition += idx * 131072;
                chunks.Add(chunkStartPosition);
            }

            while ((chunkStartPosition + 1048576) < size)
            {
                chunkStartPosition += 1048576;
                chunks.Add(chunkStartPosition);
            }

            return chunks.ToArray();
        }
    }
}
