namespace CG.Web.MegaApiClient
{
  using System;
  using System.IO;
  using System.Threading;

  public class CancellableStream : Stream
  {
    private Stream _stream;
    private readonly CancellationToken _cancellationToken;

    public CancellableStream(Stream stream, CancellationToken cancellationToken)
    {
      _stream = stream ?? throw new ArgumentNullException(nameof(stream));
      _cancellationToken = cancellationToken;
    }

    public override bool CanRead
    {
      get
      {
        _cancellationToken.ThrowIfCancellationRequested();
        return _stream.CanRead;
      }
    }

    public override bool CanSeek
    {
      get
      {
        _cancellationToken.ThrowIfCancellationRequested();
        return _stream.CanSeek;
      }
    }

    public override bool CanWrite
    {
      get
      {
        _cancellationToken.ThrowIfCancellationRequested();
        return _stream.CanWrite;
      }
    }

    public override void Flush()
    {
      _cancellationToken.ThrowIfCancellationRequested();
      _stream.Flush();
    }

    public override long Length
    {
      get
      {
        _cancellationToken.ThrowIfCancellationRequested();
        return _stream.Length;
      }
    }

    public override long Position
    {
      get
      {
        _cancellationToken.ThrowIfCancellationRequested();
        return _stream.Position;
      }

      set
      {
        _cancellationToken.ThrowIfCancellationRequested();
        _stream.Position = value;
      }
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
      _cancellationToken.ThrowIfCancellationRequested();
      return _stream.Read(buffer, offset, count);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
      _cancellationToken.ThrowIfCancellationRequested();
      return _stream.Seek(offset, origin);
    }

    public override void SetLength(long value)
    {
      _cancellationToken.ThrowIfCancellationRequested();
      _stream.SetLength(value);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
      _cancellationToken.ThrowIfCancellationRequested();
      _stream.Write(buffer, offset, count);
    }

    protected override void Dispose(bool disposing)
    {
      if (disposing)
      {
        _stream?.Dispose();
        _stream = null;
      }
    }
  }
}
