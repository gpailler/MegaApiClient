using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CG.Web.MegaApiClient.Tests.Context
{
  public abstract class TestContext : ITestContext
  {
    private const int WebTimeout = 60000;
    private const int MaxRetry = 3;

    private readonly Lazy<IMegaApiClient> lazyClient;

    protected TestContext()
    {
      this.Options = new Options();
      this.WebClient = new TestWebClient(new WebClient(WebTimeout), MaxRetry);
      this.lazyClient = new Lazy<IMegaApiClient>(this.InitializeClient);
    }

    public IMegaApiClient Client
    {
      get { return this.lazyClient.Value; }
    }

    public IWebClient WebClient { get; }

    public Options Options { get; }

    public IEnumerable<string> ProtectedNodes { get; protected set; }

    public IEnumerable<string> PermanentRootNodes { get; protected set; }

    protected virtual IMegaApiClient CreateClient()
    {
      return new MegaApiClient(this.Options, this.WebClient);
    }

    protected abstract void ConnectClient(IMegaApiClient client);

    private IMegaApiClient InitializeClient()
    {
      var client = this.CreateClient();
      client.ApiRequestFailed += this.OnApiRequestFailed;
      this.ConnectClient(client);

      return client;
    }

    private void OnApiRequestFailed(object sender, ApiRequestFailedEventArgs e)
    {
      Trace.WriteLine(e.ApiResult.ToString());
    }
  }
}
