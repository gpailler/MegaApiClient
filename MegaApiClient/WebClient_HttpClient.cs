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

    private readonly HttpClient httpClient;

    public WebClient(int responseTimeout = DefaultResponseTimeout, ProductInfoHeaderValue userAgent = null)
      : this(responseTimeout, userAgent, null, false)
    {
    }

    internal WebClient(int responseTimeout, ProductInfoHeaderValue userAgent, HttpMessageHandler messageHandler, bool connectionClose)
    {
      this.BufferSize = Options.DefaultBufferSize;
      this.httpClient = messageHandler == null ? new HttpClient() : new HttpClient(messageHandler);
      this.httpClient.Timeout = TimeSpan.FromMilliseconds(responseTimeout);
      this.httpClient.DefaultRequestHeaders.UserAgent.Add(userAgent ?? this.GenerateUserAgent());
      this.httpClient.DefaultRequestHeaders.ConnectionClose = connectionClose;
    }

    public int BufferSize { get; set; }

    public string PostRequestJson(Uri url, string jsonData)
    {
      using (MemoryStream jsonStream = new MemoryStream(jsonData.ToBytes()))
      {
        return this.PostRequest(url, jsonStream, "application/json");
      }
    }

    public string PostRequestRaw(Uri url, Stream dataStream)
    {
      return this.PostRequest(url, dataStream, "application/octet-stream");
    }

    public Stream GetRequestRaw(Uri url)
    {
      return this.httpClient.GetStreamAsync(url).Result;
    }

    private string PostRequest(Uri url, Stream dataStream, string contentType)
    {
      using (StreamContent content = new StreamContent(dataStream, this.BufferSize))
      {
        content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
      
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, url);
        requestMessage.Content = content;
        
        using (HttpResponseMessage response = this.httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead).Result)
        {
          if (!response.IsSuccessStatusCode
              && response.StatusCode == HttpStatusCode.InternalServerError
              && response.ReasonPhrase == "Server Too Busy")
          {
            return ((long)ApiResultCode.RequestFailedRetry).ToString();
          }
          
          response.EnsureSuccessStatusCode();
          
          using (Stream stream = response.Content.ReadAsStreamAsync().Result)
          {
            using (StreamReader streamReader = new StreamReader(stream, Encoding.UTF8))
            {
              return streamReader.ReadToEnd();
            }
          }
        }
      }
    }

    private ProductInfoHeaderValue GenerateUserAgent()
    {
      AssemblyName assemblyName = this.GetType().GetTypeInfo().Assembly.GetName();
      return new ProductInfoHeaderValue(assemblyName.Name, assemblyName.Version.ToString(2));
    }
  }
}
#endif
