#if !NET40
namespace CG.Web.MegaApiClient
{
  using System;
  using System.IO;
  using System.Net;
  using System.Reflection;
  using System.Text;
  using System.Threading;

  using System.Net.Http;
  using System.Net.Http.Headers;

  public class WebClient : IWebClient
  {
    private const int DefaultResponseTimeout = Timeout.Infinite;

    private readonly HttpClient _httpClient;

    public WebClient(int responseTimeout = DefaultResponseTimeout, ProductInfoHeaderValue userAgent = null)
      : this(responseTimeout, userAgent, null, false)
    {
    }

    internal WebClient(int responseTimeout, ProductInfoHeaderValue userAgent, HttpMessageHandler messageHandler, bool connectionClose)
    {
      BufferSize = Options.DefaultBufferSize;
      _httpClient = messageHandler == null ? new HttpClient() : new HttpClient(messageHandler);
      _httpClient.Timeout = TimeSpan.FromMilliseconds(responseTimeout);
      _httpClient.DefaultRequestHeaders.UserAgent.Add(userAgent ?? GenerateUserAgent());
      _httpClient.DefaultRequestHeaders.ConnectionClose = connectionClose;
    }

    public int BufferSize { get; set; }

    public string PostRequestJson(Uri url, string jsonData)
    {
      using (var jsonStream = new MemoryStream(jsonData.ToBytes()))
      {
        using (var responseStream = PostRequest(url, jsonStream, "application/json"))
        {
          return StreamToString(responseStream);
        }
      }
    }

    public string PostRequestRaw(Uri url, Stream dataStream)
    {
      using (var responseStream = PostRequest(url, dataStream, "application/json"))
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
      return _httpClient.GetStreamAsync(url).Result;
    }

    private Stream PostRequest(Uri url, Stream dataStream, string contentType)
    {
      using (var content = new StreamContent(dataStream, BufferSize))
      {
        content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, url)
        {
          Content = content
        };

        var response = _httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead).Result;
        if (!response.IsSuccessStatusCode
            && response.StatusCode == HttpStatusCode.InternalServerError
            && response.ReasonPhrase == "Server Too Busy")
        {
          return new MemoryStream(Encoding.UTF8.GetBytes(((long)ApiResultCode.RequestFailedRetry).ToString()));
        }

        response.EnsureSuccessStatusCode();

        return response.Content.ReadAsStreamAsync().Result;
      }
    }

    private string StreamToString(Stream stream)
    {
      using (var streamReader = new StreamReader(stream, Encoding.UTF8))
      {
        return streamReader.ReadToEnd();
      }
    }

    private ProductInfoHeaderValue GenerateUserAgent()
    {
      var assemblyName = GetType().GetTypeInfo().Assembly.GetName();
      return new ProductInfoHeaderValue(assemblyName.Name, assemblyName.Version.ToString(2));
    }
  }
}
#endif
