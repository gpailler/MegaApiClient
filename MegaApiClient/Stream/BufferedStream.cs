using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace CG.Web.MegaApiClient
{
  class BufferedStream : Stream
  {
    const int BufferSize = 65536;

    Stream innerStream;
    byte[] streamBuffer = new byte[BufferSize];
    int streamBufferDataStartIndex;
    int streamBufferDataCount;

    public BufferedStream(Stream innerStream)
    {
      this.innerStream = innerStream;
    }

    public byte[] Buffer => streamBuffer;
    public int BufferOffset => streamBufferDataStartIndex;
    public int AvailableCount => streamBufferDataCount;

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
        var copyCount = Math.Min(this.streamBufferDataCount, count);
        if (copyCount != 0)
        {
          Array.Copy(this.streamBuffer, this.streamBufferDataStartIndex, buffer, offset, copyCount);
          offset += copyCount;
          count -= copyCount;
          this.streamBufferDataStartIndex += copyCount;
          this.streamBufferDataCount -= copyCount;
          totalReadCount += copyCount;
        }

        if (count == 0) break; // Request has been filled.

        Debug.Assert(this.streamBufferDataCount == 0); // Buffer is currently empty.

        this.streamBufferDataStartIndex = 0;
        this.streamBufferDataCount = 0;

        FillBuffer();

        if (this.streamBufferDataCount == 0) break; // End of stream.
      }

      return totalReadCount;
    }

    public void FillBuffer()
    {
      while (true)
      {
        var startOfFreeSpace = this.streamBufferDataStartIndex + this.streamBufferDataCount;

        var availableSpaceInBuffer = this.streamBuffer.Length - startOfFreeSpace;
        if (availableSpaceInBuffer == 0) break; // Buffer is full.

        var readCount = innerStream.Read(this.streamBuffer, startOfFreeSpace, availableSpaceInBuffer);
        if (readCount == 0) break; // End of stream.

        this.streamBufferDataCount += readCount;
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
