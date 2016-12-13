using System;
using System.Collections.Generic;
using Xunit.Abstractions;

namespace CG.Web.MegaApiClient.Tests.Context
{
  public abstract class TestContext : ITestContext
  {
    private const int WebTimeout = 60000;
    private const int MaxRetry = 3;

    private readonly Lazy<IMegaApiClient> lazyClient;
    private ITestOutputHelper testOutputHelper;

    protected TestContext()
    {
      this.lazyClient = new Lazy<IMegaApiClient>(this.InitializeClient);
    }

    public IMegaApiClient Client
    {
      get { return this.lazyClient.Value; }
    }

    public IWebClient WebClient { get; private set; }

    public Options Options { get; private set; }

    public IEnumerable<string> ProtectedNodes { get; protected set; }

    public IEnumerable<string> PermanentRootNodes { get; protected set; }

    public void AssignLogger(ITestOutputHelper testOutputHelper)
    {
      this.testOutputHelper = testOutputHelper;
    }

    protected virtual IMegaApiClient CreateClient()
    {
      this.Options = new Options();
      this.WebClient = new TestWebClient(new WebClient(WebTimeout), MaxRetry, this.testOutputHelper);

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
      this.testOutputHelper.WriteLine($"ApiRequestFailed: {e.ApiResult}, {e.ApiUrl}, {e.AttemptNum}, {e.DelayMilliseconds}ms, {e.ResponseJson}, {e.Exception} {e.Exception?.Message}");
    }
  }
}
