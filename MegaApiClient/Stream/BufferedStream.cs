using System;
using System.Diagnostics;
using System.IO;

namespace CG.Web.MegaApiClient
{
  internal class BufferedStream : Stream
  {
    private const int BufferSize = 65536;
    private readonly Stream _innerStream;
    private readonly byte[] _streamBuffer = new byte[BufferSize];
    private int _streamBufferDataStartIndex;
    private int _streamBufferDataCount;

    public BufferedStream(Stream innerStream)
    {
      _innerStream = innerStream;
    }

    public byte[] Buffer => _streamBuffer;
    public int BufferOffset => _streamBufferDataStartIndex;
    public int AvailableCount => _streamBufferDataCount;

    public override void Flush()
    {
      throw new NotImplementedException();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
      throw new NotImplementedException();
    }

    public override void SetLength(long value)
    {
      throw new NotImplementedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
      var totalReadCount = 0;

      while (true)
      {
        var copyCount = Math.Min(_streamBufferDataCount, count);
        if (copyCount != 0)
        {
          Array.Copy(_streamBuffer, _streamBufferDataStartIndex, buffer, offset, copyCount);
          offset += copyCount;
          count -= copyCount;
          _streamBufferDataStartIndex += copyCount;
          _streamBufferDataCount -= copyCount;
          totalReadCount += copyCount;
        }

        if (count == 0)
        {
          break; // Request has been filled.
        }

        Debug.Assert(_streamBufferDataCount == 0); // Buffer is currently empty.

        _streamBufferDataStartIndex = 0;
        _streamBufferDataCount = 0;

        FillBuffer();

        if (_streamBufferDataCount == 0)
        {
          break; // End of stream.
        }
      }

      return totalReadCount;
    }

    public void FillBuffer()
    {
      while (true)
      {
        var startOfFreeSpace = _streamBufferDataStartIndex + _streamBufferDataCount;

        var availableSpaceInBuffer = _streamBuffer.Length - startOfFreeSpace;
        if (availableSpaceInBuffer == 0)
        {
          break; // Buffer is full.
        }

        var readCount = _innerStream.Read(_streamBuffer, startOfFreeSpace, availableSpaceInBuffer);
        if (readCount == 0)
        {
          break; // End of stream.
        }

        _streamBufferDataCount += readCount;
      }
    }

    public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotImplementedException();
    public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
  }
}
