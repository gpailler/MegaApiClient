using Xunit.Abstractions;

namespace CG.Web.MegaApiClient.Tests
{
  using System;
  using System.IO;
  using System.Net;
  using System.Net.Sockets;
  using System.Threading.Tasks;
  using Polly;

  internal class TestWebClient : IWebClient
  {
    private readonly IWebClient _webClient;
    private readonly Policy _policy;
    private readonly ITestOutputHelper _testOutputHelper;

    public TestWebClient(IWebClient webClient, int maxRetry, ITestOutputHelper testOutputHelper)
    {
      this._webClient = webClient;
      this._policy = Policy
        .Handle<WebException>()
        .Or<SocketException>()
        .Or<TaskCanceledException>()
        .Or<AggregateException>()
        .WaitAndRetry(maxRetry, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), this.OnRetry);
      this._testOutputHelper = testOutputHelper;
    }

    public enum CallType
    {
      PostRequestJson,
      PostRequestRaw,
      GetRequestRaw
    }

    public event Action<CallType, Uri> OnCalled;

    public int BufferSize
    {
      get { return this._webClient.BufferSize; }
      set { this._webClient.BufferSize = value; }
    }

    public string PostRequestJson(Uri url, string jsonData)
    {
      return this._policy.Execute(() =>
      {
        this.OnCalled?.Invoke(CallType.PostRequestJson, url);
        return this._webClient.PostRequestJson(url, jsonData);
      });
    }

    public string PostRequestRaw(Uri url, Stream dataStream)
    {
      return this._policy.Execute(() =>
      {
        this.OnCalled?.Invoke(CallType.PostRequestRaw, url);

        // Create a copy of the stream because webClient can dispose it
        // It's useful in case of retries
        Stream dataStreamCopy = this.CloneStream(dataStream);

        return this._webClient.PostRequestRaw(url, dataStreamCopy);
      });
    }

    public Stream GetRequestRaw(Uri url)
    {
      return this._policy.Execute(() =>
      {
        this.OnCalled?.Invoke(CallType.GetRequestRaw, url);
        return this._webClient.GetRequestRaw(url);
      });
    }

    private Stream CloneStream(Stream dataStream)
    {
      byte[] buffer = new byte[dataStream.Length];
      MemoryStream cloneStream = new MemoryStream(buffer);
      dataStream.CopyTo(cloneStream);

      dataStream.Position = 0;
      cloneStream.Position = 0;

      return cloneStream;
    }

    private void OnRetry(Exception ex, TimeSpan ts)
    {
      if (ex is AggregateException aEx)
      {
        this._testOutputHelper.WriteLine("AggregateException...");
        ex = aEx.InnerException;

        if (ex is TaskCanceledException tEx)
        {
          this._testOutputHelper.WriteLine("TaskCanceledException...");
          if (tEx.InnerException != null)
          {
            ex = tEx.InnerException;
          }
        }
      }

      this._testOutputHelper.WriteLine($"Request failed: {ts.TotalSeconds}, {ex}, {ex.Message}");
    }
  }
}
