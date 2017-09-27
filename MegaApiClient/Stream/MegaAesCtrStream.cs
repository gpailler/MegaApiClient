namespace CG.Web.MegaApiClient
{
  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.Linq;
  using System.Security.Cryptography;

  internal class MegaAesCtrStreamCrypter : MegaAesCtrStream
  {
    public MegaAesCtrStreamCrypter(Stream stream)
      : base(stream, stream.Length, Mode.Crypt, Crypto.CreateAesKey(), Crypto.CreateAesKey().CopySubArray(8))
    {
    }

    public byte[] FileKey
    {
      get { return this.fileKey; }
    }

    public byte[] Iv
    {
      get { return this.iv; }
    }

    public byte[] MetaMac
    {
      get
      {
        if (this.position != this.streamLength)
        {
          throw new NotSupportedException("Stream must be fully read to obtain computed FileMac");
        }

        return this.metaMac;
      }
    }
  }

  internal class MegaAesCtrStreamDecrypter : MegaAesCtrStream
  {
    private readonly byte[] expectedMetaMac;

    public MegaAesCtrStreamDecrypter(Stream stream, long streamLength, byte[] fileKey, byte[] iv, byte[] expectedMetaMac)
      : base(stream, streamLength, Mode.Decrypt, fileKey, iv)
    {
      if (expectedMetaMac == null || expectedMetaMac.Length != 8)
      {
        throw new ArgumentException("Invalid expectedMetaMac");
      }

      this.expectedMetaMac = expectedMetaMac;
    }

    protected override void OnStreamRead()
    {
      if (!this.expectedMetaMac.SequenceEqual(this.metaMac))
      {
        throw new DownloadException();
      }
    }
  }

  internal abstract class MegaAesCtrStream : Stream
  {
    protected readonly byte[] fileKey;
    protected readonly byte[] iv;
    protected readonly long streamLength;
    protected long position = 0;
    protected byte[] metaMac = new byte[8];

    private readonly Stream stream;
    private readonly Mode mode;
    private readonly HashSet<long> chunksPositionsCache;
    private readonly byte[] counter = new byte[8];
    private readonly ICryptoTransform encryptor;
    private long currentCounter = 0;
    private byte[] currentChunkMac = new byte[16];
    private byte[] fileMac = new byte[16];

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

      this.stream = stream;
      this.streamLength = streamLength;
      this.mode = mode;
      this.fileKey = fileKey;
      this.iv = iv;

      this.ChunksPositions = this.GetChunksPositions(this.streamLength).ToArray();
      this.chunksPositionsCache = new HashSet<long>(this.ChunksPositions);

      this.encryptor = Crypto.CreateAesEncryptor(this.fileKey);
    }

    protected override void Dispose(bool disposing)
    {
      base.Dispose(disposing);
      this.encryptor.Dispose();
    }

    protected enum Mode
    {
      Crypt,
      Decrypt
    }

    public long[] ChunksPositions { get; }

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
      get { return this.streamLength; }
    }

    public override long Position
    {
      get
      {
        return this.position;
      }

      set
      {
        if (this.position != value)
        {
          throw new NotSupportedException("Seek is not supported");
        }
      }
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
      if (this.position == this.streamLength)
      {
        return 0;
      }

      for (long pos = this.position; pos < Math.Min(this.position + count, this.streamLength); pos += 16)
      {
        // We are on a chunk bondary
        if (this.chunksPositionsCache.Contains(pos))
        {
          if (pos != 0)
          {
            // Compute the current chunk mac data on each chunk bondary
            this.ComputeChunk(encryptor);
          }

          // Init chunk mac with Iv values
          for (int i = 0; i < 8; i++)
          {
            this.currentChunkMac[i] = this.iv[i];
            this.currentChunkMac[i + 8] = this.iv[i];
          }
        }

        this.IncrementCounter();

        // Iterate each AES 16 bytes block
        byte[] input = new byte[16];
        byte[] output = new byte[input.Length];
        int inputLength = this.stream.Read(input, 0, input.Length);
        if (inputLength != input.Length)
        {
          // Sometimes, the stream is not finished but the read is not complete
          inputLength += this.stream.Read(input, inputLength, input.Length - inputLength);
        }

        // Merge Iv and counter
        byte[] ivCounter = new byte[16];
        Array.Copy(this.iv, ivCounter, 8);
        Array.Copy(this.counter, 0, ivCounter, 8, 8);

        byte[] encryptedIvCounter = Crypto.EncryptAes(ivCounter, encryptor);

        for (int inputPos = 0; inputPos < inputLength; inputPos++)
        {
          output[inputPos] = (byte)(encryptedIvCounter[inputPos] ^ input[inputPos]);
          this.currentChunkMac[inputPos] ^= (this.mode == Mode.Crypt) ? input[inputPos] : output[inputPos];
        }

        // Copy to buffer
        Array.Copy(output, 0, buffer, (int)(offset + pos - this.position), (int)Math.Min(output.Length, this.streamLength - pos));

        // Crypt to current chunk mac
        this.currentChunkMac = Crypto.EncryptAes(this.currentChunkMac, encryptor);
      }

      long len = Math.Min(count, this.streamLength - this.position);
      this.position += len;

      // When stream is fully processed, we compute the last chunk
      if (this.position == this.streamLength)
      {
        this.ComputeChunk(encryptor);

        // Compute Meta MAC
        for (int i = 0; i < 4; i++)
        {
          this.metaMac[i] = (byte)(this.fileMac[i] ^ this.fileMac[i + 4]);
          this.metaMac[i + 4] = (byte)(this.fileMac[i + 8] ^ this.fileMac[i + 12]);
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

    protected virtual void OnStreamRead()
    {
    }

    private void IncrementCounter()
    {
      byte[] counter = BitConverter.GetBytes(this.currentCounter++);
      if (BitConverter.IsLittleEndian)
      {
        Array.Reverse(counter);
      }

      Array.Copy(counter, this.counter, 8);
    }

    private void ComputeChunk(ICryptoTransform encryptor)
    {
      for (int i = 0; i < 16; i++)
      {
        this.fileMac[i] ^= this.currentChunkMac[i];
      }

      this.fileMac = Crypto.EncryptAes(this.fileMac, encryptor);
    }

    private IEnumerable<long> GetChunksPositions(long size)
    {
      yield return 0;

      long chunkStartPosition = 0;
      for (int idx = 1; (idx <= 8) && (chunkStartPosition < (size - (idx * 131072))); idx++)
      {
        chunkStartPosition += idx * 131072;
        yield return chunkStartPosition;
      }

      while ((chunkStartPosition + 1048576) < size)
      {
        chunkStartPosition += 1048576;
        yield return chunkStartPosition;
      }
    }
  }
}
