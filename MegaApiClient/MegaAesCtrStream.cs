using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CG.Web.MegaApiClient
{
    internal class MegaAesCtrStreamCrypter : MegaAesCtrStream
    {
        public MegaAesCtrStreamCrypter(Stream stream)
            : base(stream, Mode.Crypt, Crypto.CreateAesKey(), Crypto.CreateAesKey())
        {
        }
    }

    internal class MegaAesCtrStreamDecrypter : MegaAesCtrStream
    {
        public MegaAesCtrStreamDecrypter(Stream stream, byte[] fileKey, byte[] counter)
            : base(stream, Mode.Crypt, fileKey, counter)
        {
            throw new NotImplementedException();
        }
    }

    internal abstract class MegaAesCtrStream : Stream
    {
        private readonly Stream _stream;
        private readonly Mode _mode;

        private readonly long[] _chunksPositions;
        private readonly byte[] _fileKey;
        private readonly byte[] _counter;
        private byte[] _fileMac = new byte[16];

        private long _position = 0;
        private long _currentCounter = 0;
        private byte[] _currentChunkMac = new byte[16];

        protected MegaAesCtrStream(Stream stream, Mode mode, byte[] fileKey, byte[] counter)
        {
            this._stream = stream;
            this._mode = mode;
            this._fileKey = fileKey;
            this._counter = counter;

            this._chunksPositions = GetChunksPositions(stream.Length);
        }
        
        public byte[] FileKey { get { return this._fileKey; } }

        public byte[] Counter { get { return this._counter; } }

        public byte[] FileMac { get { return this._fileMac; } }
        
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (this._position == this._stream.Length)
            {
                return 0;
            }

            for (long pos = this._position; pos < Math.Min(this._position + count, this._stream.Length); pos += 16)
            {
                // We are on a chunk bondary
                if (this._chunksPositions.Any(chunk => chunk == pos) || pos == 0)
                {
                    if (pos != 0)
                    {
                        // Compute the current chunk mac data
                        this.ComputeChunk();
                    }

                    // Init chunk mac with counter values
                    for (int i = 0; i < 8; i++)
                    {
                        _currentChunkMac[i] = this._counter[i];
                        _currentChunkMac[i + 8] = this._counter[i];
                    }
                }

                // Update counter
                this.IncrementCounter();

                // Iterate each AES 16 bytes block
                byte[] input = new byte[16];
                byte[] output = new byte[input.Length];
                int inputLength = this._stream.Read(input, 0, input.Length);

                byte[] encryptedCounter = Crypto.EncryptAes(this._counter, this._fileKey);

                for (int inputPos = 0; inputPos < inputLength; inputPos++)
                {
                    output[inputPos] = (byte)(encryptedCounter[inputPos] ^ input[inputPos]);
                    this._currentChunkMac[inputPos] ^= input[inputPos];
                }

                // Copy to buffer
                Array.Copy(output, 0, buffer, pos - this._position, Math.Min(output.Length, this._stream.Length - pos));

                // Crypt to current chunk mac
                this._currentChunkMac = Crypto.EncryptAes(this._currentChunkMac, this._fileKey);
            }

            long len = (long)Math.Min(count, this._stream.Length - this._position);
            this._position += len;

            // When file is fully encrypted, we compute the last chunk
            if (this._position == this._stream.Length)
            {
                this.ComputeChunk();
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
            get { return this._stream.Length; }
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

        private void IncrementCounter()
        {
            byte[] bitCounter = BitConverter.GetBytes(_currentCounter++);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bitCounter);
            }
            Array.Copy(bitCounter, 0, this._counter, 8, 8);
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
