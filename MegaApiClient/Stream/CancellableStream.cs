namespace CG.Web.MegaApiClient
{
  using System;
  using System.IO;
  using System.Threading;

  public class CancellableStream : Stream
  {
    private Stream stream;
    private readonly CancellationToken cancellationToken;

    public CancellableStream(Stream stream, CancellationToken cancellationToken)
    {
      if (stream == null)
      {
        throw new ArgumentNullException(nameof(stream));
      }

      this.stream = stream;
      this.cancellationToken = cancellationToken;
    }

    public override bool CanRead
    {
      get
      {
        this.cancellationToken.ThrowIfCancellationRequested();
        return this.stream.CanRead;
      }
    }

    public override bool CanSeek
    {
      get
      {
        this.cancellationToken.ThrowIfCancellationRequested();
        return this.stream.CanSeek;
      }
    }

    public override bool CanWrite
    {
      get
      {
        this.cancellationToken.ThrowIfCancellationRequested();
        return this.stream.CanWrite;
      }
    }

    public override void Flush()
    {
      this.cancellationToken.ThrowIfCancellationRequested();
      this.stream.Flush();
    }

    public override long Length
    {
      get
      {
        this.cancellationToken.ThrowIfCancellationRequested();
        return this.stream.Length;
      }
    }

    public override long Position
    {
      get
      {
        this.cancellationToken.ThrowIfCancellationRequested();
        return this.stream.Position;
      }

      set
      {
        this.cancellationToken.ThrowIfCancellationRequested();
        this.stream.Position = value;
      }
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
      this.cancellationToken.ThrowIfCancellationRequested();
      return this.stream.Read(buffer, offset, count);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
      this.cancellationToken.ThrowIfCancellationRequested();
      return this.stream.Seek(offset, origin);
    }

    public override void SetLength(long value)
    {
      this.cancellationToken.ThrowIfCancellationRequested();
      this.stream.SetLength(value);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
      this.cancellationToken.ThrowIfCancellationRequested();
      this.stream.Write(buffer, offset, count);
    }

#if !NETCORE
    public override void Close()
    {
      this.stream?.Close();

      base.Close();
    }
#endif

    protected override void Dispose(bool disposing)
    {
      if (disposing)
      {
        this.stream?.Dispose();
        this.stream = null;
      }
    }
  }
}