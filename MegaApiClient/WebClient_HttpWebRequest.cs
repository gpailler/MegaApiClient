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

    private readonly int _responseTimeout;
    private readonly string _userAgent;

    public WebClient(int responseTimeout = DefaultResponseTimeout, string userAgent = null)
    {
      if (!ServicePointManager.SecurityProtocol.HasFlag((SecurityProtocolType)3072))
      {
        throw new NotSupportedException("mega.nz API requires support for TLS v1.2 or higher. Check https://gpailler.github.io/MegaApiClient/#compatibility for additional information");
      }

      BufferSize = Options.DefaultBufferSize;
      _responseTimeout = responseTimeout;
      _userAgent = userAgent ?? GenerateUserAgent();
    }

    public int BufferSize { get; set; }

    public string PostRequestJson(Uri url, string jsonData)
    {
      return PostRequestJson(url, jsonData, null);
    }

    public string PostRequestJson(Uri url, string jsonData, string hashcash)
    {
      using (var jsonStream = new MemoryStream(jsonData.ToBytes()))
      {
        using (var responseStream = PostRequest(url, jsonStream, "application/json", hashcash))
        {
          return StreamToString(responseStream);
        }
      }
    }

    public string PostRequestRaw(Uri url, Stream dataStream)
    {
      using (var responseStream = PostRequest(url, dataStream, "application/octet-stream"))
      {
        return StreamToString(responseStream);
      }
    }

    public Stream PostRequestRawAsStream(Uri url, Stream dataStream)
    {
      return PostRequest(url, dataStream, "application/octet-stream");
    }

    public Stream GetRequestRaw(Uri url)
    {
      var request = CreateRequest(url);
      request.Method = "GET";

      return request.GetResponse().GetResponseStream();
    }

    private Stream PostRequest(Uri url, Stream dataStream, string contentType)
    {
      return PostRequest(url, dataStream, contentType, null);
    }

    private Stream PostRequest(Uri url, Stream dataStream, string contentType, string hashcash)
    {
      var request = CreateRequest(url);
      request.ContentLength = dataStream.Length;
      request.Method = "POST";
      request.ContentType = contentType;
      if(hashcash != null)
      {
        request.Headers.Add("X-Hashcash", hashcash);
      }

      using (var requestStream = request.GetRequestStream())
      {
        dataStream.Position = 0;
        dataStream.CopyTo(requestStream, BufferSize);
      }

      var response = (HttpWebResponse)request.GetResponse();
      return response.GetResponseStream();
    }

    private HttpWebRequest CreateRequest(Uri url)
    {
      var request = (HttpWebRequest)WebRequest.Create(url);
      request.Timeout = _responseTimeout;
      request.UserAgent = _userAgent;

      return request;
    }

    private string GenerateUserAgent()
    {
      var assemblyName = Assembly.GetExecutingAssembly().GetName();
      return string.Format("{0} v{1}", assemblyName.Name, assemblyName.Version.ToString(2));
    }

    private string StreamToString(Stream stream)
    {
      using (var streamReader = new StreamReader(stream, Encoding.UTF8))
      {
        return streamReader.ReadToEnd();
      }
    }
  }
}
#endif
