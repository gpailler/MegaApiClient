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
    private readonly Action<string> _logMessageAction;

    public TestWebClient(IWebClient webClient, int maxRetry, Action<string> _logMessageAction)
    {
      this._webClient = webClient;
      this._policy = Policy
        .Handle<WebException>()
        .Or<SocketException>()
        .Or<TaskCanceledException>()
        .Or<AggregateException>()
        .WaitAndRetry(maxRetry, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), this.OnRetry);
      this._logMessageAction = _logMessageAction;
    }

    public enum CallType
    {
      PostRequestJson,
      PostRequestRaw,
      PostRequestRawAsStream,
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
        var result = this._webClient.PostRequestJson(url, jsonData);
        this.OnCalled?.Invoke(CallType.PostRequestJson, url);

        return result;
      });
    }

    public string PostRequestRaw(Uri url, Stream dataStream)
    {
      return this._policy.Execute(() =>
      {
        // Create a copy of the stream because webClient can dispose it
        // It's useful in case of retries
        Stream dataStreamCopy = this.CloneStream(dataStream);

        var result = this._webClient.PostRequestRaw(url, dataStreamCopy);
        this.OnCalled?.Invoke(CallType.PostRequestRaw, url);

        return result;
      });
    }

    public Stream PostRequestRawAsStream(Uri url, Stream dataStream)
    {
      return this._policy.Execute(() =>
      {
        // Create a copy of the stream because webClient can dispose it
        // It's useful in case of retries
        Stream dataStreamCopy = this.CloneStream(dataStream);

        var result = this._webClient.PostRequestRawAsStream(url, dataStreamCopy);
        this.OnCalled?.Invoke(CallType.PostRequestRawAsStream, url);

        return result;
      });
    }

    public Stream GetRequestRaw(Uri url)
    {
      return this._policy.Execute(() =>
      {
        var result = this._webClient.GetRequestRaw(url);
        this.OnCalled?.Invoke(CallType.GetRequestRaw, url);

        return result;
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
        this._logMessageAction("AggregateException...");
        ex = aEx.InnerException;

        if (ex is TaskCanceledException tEx)
        {
          this._logMessageAction("TaskCanceledException...");
          if (tEx.InnerException != null)
          {
            ex = tEx.InnerException;
          }
        }
      }

      this._logMessageAction($"Request failed: {ts.TotalSeconds}, {ex}, {ex.Message}");
    }
  }
}
