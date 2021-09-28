#if !NET40
namespace CG.Web.MegaApiClient
{
  using System.IO;
  using System;

  internal class ProgressionStream : Stream
  {
    private readonly Stream _baseStream;
    private readonly IProgress<double> _progress;
    private readonly long _reportProgressChunkSize;

    private long _chunkSize;

    public ProgressionStream(Stream baseStream, IProgress<double> progress, long reportProgressChunkSize)
    {
      _baseStream = baseStream;
      _progress = progress ?? new Progress<double>();
      _reportProgressChunkSize = reportProgressChunkSize;
    }

    public override int Read(byte[] array, int offset, int count)
    {
      var bytesRead = _baseStream.Read(array, offset, count);
      ReportProgress(bytesRead);

      return bytesRead;
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
      _baseStream.Write(buffer, offset, count);
    }

    protected override void Dispose(bool disposing)
    {
      base.Dispose(disposing);

      // Report 100% progress only if it was not already sent
      if (_chunkSize != 0)
      {
        _progress.Report(100);
      }
    }

#region Forwards

    public override void Flush()
    {
      _baseStream.Flush();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
      return _baseStream.Seek(offset, origin);
    }

    public override void SetLength(long value)
    {
      _baseStream.SetLength(value);
    }

    public override bool CanRead => _baseStream.CanRead;

    public override bool CanSeek => _baseStream.CanSeek;

    public override bool CanWrite => _baseStream.CanWrite;

    public override long Length => _baseStream.Length;

    public override long Position
    {
      get => _baseStream.Position;
      set => _baseStream.Position = value;
    }

#endregion

    private void ReportProgress(int count)
    {
      _chunkSize += count;
      if (_chunkSize >= _reportProgressChunkSize)
      {
        _chunkSize = 0;
        _progress.Report(Position / (double)Length * 100);
      }
    }
  }
}
#endif
