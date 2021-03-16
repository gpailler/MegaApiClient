#if NET40
namespace CG.Web.MegaApiClient
{
  using System;
  using System.IO;
  using System.Net;
  using System.Reflection;
  using System.Text;
  using System.Threading;

  public class WebClient : IWebClient
  {
    private const int DefaultResponseTimeout = Timeout.Infinite;

    private readonly int responseTimeout;
    private readonly string userAgent;

    public WebClient(int responseTimeout = DefaultResponseTimeout, string userAgent = null)
    {
      this.BufferSize = Options.DefaultBufferSize;
      this.responseTimeout = responseTimeout;
      this.userAgent = userAgent ?? this.GenerateUserAgent();
    }

    public int BufferSize { get; set; }

    public string PostRequestJson(Uri url, string jsonData)
    {
      using (MemoryStream jsonStream = new MemoryStream(jsonData.ToBytes()))
      {
        using (var responseStream = this.PostRequest(url, jsonStream, "application/json"))
        {
          return this.StreamToString(responseStream);
        }
      }
    }

    public string PostRequestRaw(Uri url, Stream dataStream)
    {
      using (var responseStream = this.PostRequest(url, dataStream, "application/octet-stream"))
      {
        return this.StreamToString(responseStream);
      }
    }

    public Stream PostRequestRawAsStream(Uri url, Stream dataStream)
    {
      return this.PostRequest(url, dataStream, "application/octet-stream");
    }

    public Stream GetRequestRaw(Uri url)
    {
      HttpWebRequest request = this.CreateRequest(url);
      request.Method = "GET";

      return request.GetResponse().GetResponseStream();
    }

    private Stream PostRequest(Uri url, Stream dataStream, string contentType)
    {
      HttpWebRequest request = this.CreateRequest(url);
      request.ContentLength = dataStream.Length;
      request.Method = "POST";
      request.ContentType = contentType;

      using (Stream requestStream = request.GetRequestStream())
      {
        dataStream.Position = 0;
        dataStream.CopyTo(requestStream, this.BufferSize);
      }

      HttpWebResponse response = (HttpWebResponse)request.GetResponse();
      return response.GetResponseStream();
    }

    private HttpWebRequest CreateRequest(Uri url)
    {
      HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
      request.Timeout = this.responseTimeout;
      request.UserAgent = this.userAgent;

      return request;
    }

    private string GenerateUserAgent()
    {
      AssemblyName assemblyName = Assembly.GetExecutingAssembly().GetName();
      return string.Format("{0} v{1}", assemblyName.Name, assemblyName.Version.ToString(2));
    }

    private string StreamToString(Stream stream)
    {
      using (StreamReader streamReader = new StreamReader(stream, Encoding.UTF8))
      {
        return streamReader.ReadToEnd();
      }
    }
  }
}
#endif
