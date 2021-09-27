namespace CG.Web.MegaApiClient
{
  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.Linq;
  using System.Security.Cryptography;

  using Cryptography;

  internal class MegaAesCtrStreamCrypter : MegaAesCtrStream
  {
    public MegaAesCtrStreamCrypter(Stream stream)
      : base(stream, stream.Length, Mode.Crypt, Crypto.CreateAesKey(), Crypto.CreateAesKey().CopySubArray(8))
    {
    }

    public new byte[] FileKey => base.FileKey;

    public new byte[] Iv => base.Iv;

    public new byte[] MetaMac
    {
      get
      {
        if (_position != StreamLength)
        {
          throw new NotSupportedException("Stream must be fully read to obtain computed FileMac");
        }

        return base.MetaMac;
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

      _expectedMetaMac = expectedMetaMac;
    }

    protected override void OnStreamRead()
    {
      if (!_expectedMetaMac.SequenceEqual(MetaMac))
      {
        throw new DownloadException();
      }
    }
  }

  internal abstract class MegaAesCtrStream : Stream
  {
    protected readonly byte[] FileKey;
    protected readonly byte[] Iv;
    protected readonly long StreamLength;
    protected readonly byte[] MetaMac = new byte[8];
    protected long _position = 0;

    private readonly Stream _stream;
    private readonly Mode _mode;
    private readonly HashSet<long> _chunksPositionsCache;
    private readonly byte[] _counter = new byte[8];
    private readonly ICryptoTransform _encryptor;
    private long _currentCounter = 0; // Represents the next counter value to use.
    private byte[] _currentChunkMac = new byte[16];
    private byte[] _fileMac = new byte[16];

    protected MegaAesCtrStream(Stream stream, long streamLength, Mode mode, byte[] fileKey, byte[] iv)
    {
      if (fileKey == null || fileKey.Length != 16)
      {
        throw new ArgumentException("Invalid fileKey");
      }

      if (iv == null || iv.Length != 8)
      {
        throw new ArgumentException("Invalid Iv");
      }

      _stream = stream ?? throw new ArgumentNullException(nameof(stream));
      StreamLength = streamLength;
      _mode = mode;
      FileKey = fileKey;
      Iv = iv;

      ChunksPositions = GetChunksPositions(StreamLength).ToArray();
      _chunksPositionsCache = new HashSet<long>(ChunksPositions);

      _encryptor = Crypto.CreateAesEncryptor(FileKey);
    }

    protected override void Dispose(bool disposing)
    {
      base.Dispose(disposing);
      _encryptor.Dispose();
    }

    protected enum Mode
    {
      Crypt,
      Decrypt
    }

    public long[] ChunksPositions { get; }

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => StreamLength;

    public override long Position
    {
      get => _position;

      set
      {
        if (_position != value)
        {
          throw new NotSupportedException("Seek is not supported");
        }
      }
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
      if (_position == StreamLength)
      {
        return 0;
      }

      if (_position + count < StreamLength && count < 16)
      {
        throw new NotSupportedException($"Invalid '{nameof(count)}' argument. Minimal read operation must be greater than 16 bytes (except for last read operation).");
      }

      // Validate count boundaries
      count = (_position + count < StreamLength)
        ? count - (count % 16) // Make count divisible by 16 for partial reads (as the minimal block is 16)
        : count;

      for (var pos = _position; pos < Math.Min(_position + count, StreamLength); pos += 16)
      {
        // We are on a chunk bondary
        if (_chunksPositionsCache.Contains(pos))
        {
          if (pos != 0)
          {
            // Compute the current chunk mac data on each chunk bondary
            ComputeChunk(_encryptor);
          }

          // Init chunk mac with Iv values
          for (var i = 0; i < 8; i++)
          {
            _currentChunkMac[i] = Iv[i];
            _currentChunkMac[i + 8] = Iv[i];
          }
        }

        IncrementCounter();

        // Iterate each AES 16 bytes block
        var input = new byte[16];
        var output = new byte[input.Length];
        var inputLength = _stream.Read(input, 0, input.Length);
        if (inputLength != input.Length)
        {
          // Sometimes, the stream is not finished but the read is not complete
          inputLength += _stream.Read(input, inputLength, input.Length - inputLength);
        }

        // Merge Iv and counter
        var ivCounter = new byte[16];
        Array.Copy(Iv, ivCounter, 8);
        Array.Copy(_counter, 0, ivCounter, 8, 8);

        var encryptedIvCounter = Crypto.EncryptAes(ivCounter, _encryptor);

        for (var inputPos = 0; inputPos < inputLength; inputPos++)
        {
          output[inputPos] = (byte)(encryptedIvCounter[inputPos] ^ input[inputPos]);
          _currentChunkMac[inputPos] ^= (_mode == Mode.Crypt) ? input[inputPos] : output[inputPos];
        }

        // Copy to buffer
        Array.Copy(output, 0, buffer, (int)(offset + pos - _position), (int)Math.Min(output.Length, StreamLength - pos));

        // Crypt to current chunk mac
        _currentChunkMac = Crypto.EncryptAes(_currentChunkMac, _encryptor);
      }

      var len = Math.Min(count, StreamLength - _position);
      _position += len;

      // When stream is fully processed, we compute the last chunk
      if (_position == StreamLength)
      {
        ComputeChunk(_encryptor);

        // Compute Meta MAC
        for (var i = 0; i < 4; i++)
        {
          MetaMac[i] = (byte)(_fileMac[i] ^ _fileMac[i + 4]);
          MetaMac[i + 4] = (byte)(_fileMac[i + 8] ^ _fileMac[i + 12]);
        }

        OnStreamRead();
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
      if ((_currentCounter & 0xFF) != 0xFF && (_currentCounter & 0xFF) != 0x00)
      {
        // Fast path - no wrapping.
        _counter[7]++;
      }
      else
      {
        var counter = BitConverter.GetBytes(_currentCounter);
        if (BitConverter.IsLittleEndian)
        {
          Array.Reverse(counter);
        }

        Array.Copy(counter, _counter, 8);
      }

      _currentCounter++;
    }

    private void ComputeChunk(ICryptoTransform encryptor)
    {
      for (var i = 0; i < 16; i++)
      {
        _fileMac[i] ^= _currentChunkMac[i];
      }

      _fileMac = Crypto.EncryptAes(_fileMac, encryptor);
    }

    private IEnumerable<long> GetChunksPositions(long size)
    {
      yield return 0;

      long chunkStartPosition = 0;
      for (var idx = 1; (idx <= 8) && (chunkStartPosition < (size - (idx * 131072))); idx++)
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
