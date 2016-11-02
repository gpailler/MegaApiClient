namespace CG.Web.MegaApiClient
{
  using System;
  using System.IO;
  using System.Reflection;
  using System.Text;
  using System.Threading;

  using System.Net.Http;
  using System.Net.Http.Headers;

  public class WebClient : IWebClient
  {
    private const int DefaultResponseTimeout = Timeout.Infinite;

    private readonly HttpClient httpClient = new HttpClient();

    public WebClient()
        : this(DefaultResponseTimeout)
    {
      this.BufferSize = MegaApiClient.DefaultBufferSize;
    }

    internal WebClient(int responseTimeout)
    {
      this.httpClient.Timeout = TimeSpan.FromMilliseconds(responseTimeout);
      this.httpClient.DefaultRequestHeaders.UserAgent.Add(this.GenerateUserAgent());
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
      HttpResponseMessage response = this.httpClient.GetAsync(url).Result;
      return response.Content.ReadAsStreamAsync().Result;
    }

    private string PostRequest(Uri url, Stream dataStream, string contentType)
    {
      // Copy original stream to allow retries
      // HttpClient will dispose the source stream at the end of PostAsync call
      byte[] buffer = new byte[dataStream.Length];
      using (Stream dataStreamCopy = new MemoryStream(buffer))
      {
        dataStream.CopyTo(dataStreamCopy);
        dataStreamCopy.Position = 0;

        using (StreamContent content = new StreamContent(dataStreamCopy, this.BufferSize))
        {
          content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
          using (HttpResponseMessage response = this.httpClient.PostAsync(url, content).Result)
          {
            using (Stream stream = response.Content.ReadAsStreamAsync().Result)
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
      AssemblyName assemblyName = Assembly.GetExecutingAssembly().GetName();
      return new ProductInfoHeaderValue(assemblyName.Name, assemblyName.Version.ToString(2));
    }
  }
}
