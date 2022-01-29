---
uid: faq
---

### How can I download a file into a specific location?
You can use a stream with the <xref:CG.Web.MegaApiClient.MegaApiClient.Download*> method to control the destination of your download
```csharp
var destination = @"SubFolder\MyFile.jpg";
using (var stream = client.Download(node))
{
  using (var fileStream = new FileStream(destination, FileMode.CreateNew))
  {
    stream.CopyTo(fileStream);
  }
}
```


### How can I retrieve progression during download/upload?
You have several options:
- Use async methods like <xref:CG.Web.MegaApiClient.MegaApiClient.DownloadAsync*> / <xref:CG.Web.MegaApiClient.MegaApiClient.UploadAsync*>

```csharp
async void Main()
{
  var client = new MegaApiClient();
  client.LoginAnonymous();

  Uri fileLink = new Uri("https://mega.nz/#!bkwkHC7D!AWJuto8_fhleAI2WG0RvACtKkL_s9tAtvBXXDUp2bQk");
  INode node = client.GetNodeFromLink(fileLink);

  IProgress<double> progressHandler = new Progress<double>(x => Console.WriteLine("{0}%", x));
  await client.DownloadFileAsync(fileLink, node.Name, progressHandler);

  client.Logout();
}
```


- Use the methods <xref:CG.Web.MegaApiClient.MegaApiClient.Download*> / <xref:CG.Web.MegaApiClient.MegaApiClient.Upload*> with a custom stream implementation to retrieve progression

```csharp
// Upload
using (var fileStream = new FileStream(filepath, FileMode.Open))
using (var progressionStream = new ProgressionStream(fileStream, x => Console.WriteLine("{0}%", x)))
{
  client.Upload(progressionStream, filename, parent);
}

// Download
using (var downloadStream = client.Download(node))
using (var progressionStream = new ProgressionStream(downloadStream, x => Console.WriteLine("{0}%", x)))
using (var fileStream = new FileStream(filePath, FileMode.Create))
{
  progressionStream.CopyTo(fileStream);
}

public class ProgressionStream : Stream
{
  public delegate void ProgressionHandler(double progression);

  private Stream _sourceStream;
  private ProgressionHandler _progressionHandler;

  public ProgressionStream(Stream sourceStream, ProgressionHandler progressionHandler)
  {
    this._sourceStream = sourceStream;
    this._progressionHandler = progressionHandler;
  }

  public override int Read(byte[] array, int offset, int count)
  {
    this._progressionHandler(this.Position / (double)this.Length * 100);

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
```


### How can I control download/upload speed?
You can use a custom Stream implementation with <xref:CG.Web.MegaApiClient.MegaApiClient.Download*> / <xref:CG.Web.MegaApiClient.MegaApiClient.Upload*> methods.
Take a look on [ThrottledStream](https://github.com/duplicati/duplicati/blob/master/Duplicati/Library/Utility/ThrottledStream.cs) implementation from Duplicati project.


### How can I customize MegaApiClient options (application key, buffers)?
When creating the MegaApiClient, you can pass a custom configuration class called <xref:CG.Web.MegaApiClient.Options>.


### How can I fix UWP error during login ?
UWP compiler has some restriction and you can face some unexpected exceptions during login.
Take a look on [GitHub issue #69](https://github.com/gpailler/MegaApiClient/issues/69#issuecomment-326811805) for details.


### How can I use this library when I have a proxy with authentication?
You can add the following line to specify proxy credentials for the whole application.
```csharp
WebRequest.DefaultWebProxy.Credentials = New NetworkCredentails("Username", "Password")
```


### What is 509 error code / MEGA's 5GB download restriction?
Free accounts on Mega have a 5GB download restriction per day. If you want to download more than 5GB, you need a paid account.
