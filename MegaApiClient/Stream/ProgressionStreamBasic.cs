using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CG.Web.MegaApiClient
{
  public class ProgressionStreamBasic : Stream
  {
    public delegate void ProgressionHandler(long progression);

    private Stream _sourceStream;
    private ProgressionHandler _progressionHandler;

    public ProgressionStreamBasic(Stream sourceStream, ProgressionHandler progressionHandler)
    {
      this._sourceStream = sourceStream;
      this._progressionHandler = progressionHandler;
    }

    public override int Read(byte[] array, int offset, int count)
    {
      new Thread(() =>
      {

        this._progressionHandler(this.Position);

      }).Start();

      return this._sourceStream.Read(array, offset, count);
    }
    public override bool CanRead
    {
      get { return this._sourceStream.CanRead; }
    }

    public override bool CanSeek
    {
      get { return this._sourceStream.CanSeek; }
    }

    public override bool CanWrite
    {
      get { return this._sourceStream.CanWrite; }
    }

    public override long Length
    {
      get { return this._sourceStream.Length; }
    }

    public override long Position
    {
      get { return this._sourceStream.Position; }
      set { this._sourceStream.Position = value; }
    }

    public override void Flush()
    {
      this._sourceStream.Flush();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
      return this._sourceStream.Seek(offset, origin);
    }

    public override void SetLength(long value)
    {
      this._sourceStream.SetLength(value);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
      this._sourceStream.Write(buffer, offset, count);
    }
  }
}
