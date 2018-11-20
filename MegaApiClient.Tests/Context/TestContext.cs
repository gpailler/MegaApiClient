using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace CG.Web.MegaApiClient.Tests.Context
{
  public abstract class TestContext : ITestContext
  {
    private const int MaxRetry = 3;

    private readonly Lazy<IMegaApiClient> lazyClient;
    private readonly Lazy<IEnumerable<string>> lazyProtectedNodes;
    private readonly Lazy<IEnumerable<string>> lazyPermanentNodes;
    private readonly Action<string> logMessageAction;
    private ITestOutputHelper testOutputHelper;

    protected TestContext()
    {
      this.WebTimeout = 60000;
      this.lazyClient = new Lazy<IMegaApiClient>(this.InitializeClient);
      this.lazyProtectedNodes = new Lazy<IEnumerable<string>>(() => this.GetProtectedNodes().ToArray());
      this.lazyPermanentNodes = new Lazy<IEnumerable<string>>(() => this.GetPermanentNodes().ToArray());
      this.logMessageAction = x => testOutputHelper?.WriteLine(x);
    }

    public IMegaApiClient Client
    {
      get { return this.lazyClient.Value; }
    }

    public IWebClient WebClient { get; private set; }

    public Options Options { get; private set; }

    public int WebTimeout { get; }

    public IEnumerable<string> ProtectedNodes
    {
      get { return this.lazyProtectedNodes.Value; }
    }

    public IEnumerable<string> PermanentRootNodes
    {
      get { return this.lazyPermanentNodes.Value; }
    }

    public void SetLogger(ITestOutputHelper testOutputHelper)
    {
      this.testOutputHelper = testOutputHelper;
    }

    public void ClearLogger()
    {
      this.testOutputHelper = null;
    }

    protected virtual IMegaApiClient CreateClient()
    {
      this.Options = new Options(applicationKey: "ewZQFBBC");
      this.WebClient = new TestWebClient(new WebClient(this.WebTimeout, null, new TestMessageHandler(this.logMessageAction)), MaxRetry, this.logMessageAction);

      return new MegaApiClient(this.Options, this.WebClient);
    }

    protected abstract IEnumerable<string> GetProtectedNodes();

    protected abstract IEnumerable<string> GetPermanentNodes();

    protected abstract void ConnectClient(IMegaApiClient client);

    private IMegaApiClient InitializeClient()
    {
      var client = this.CreateClient();
      client.ApiRequestFailed += this.OnApiRequestFailed;
      this.ConnectClient(client);

      this.logMessageAction($"Client created for context {this.GetType().Name}");

      return client;
    }

    private void OnApiRequestFailed(object sender, ApiRequestFailedEventArgs e)
    {
      this.logMessageAction($"ApiRequestFailed: {e.ApiResult}, {e.ApiUrl}, {e.AttemptNum}, {e.DelayMilliseconds}ms, {e.ResponseJson}, {e.Exception} {e.Exception?.Message}");
    }

    private class TestMessageHandler : HttpClientHandler
    {
      private readonly Action<string> logMessageAction;

      public TestMessageHandler(Action<string> logMessageAction)
      {
        this.logMessageAction = logMessageAction;
      }

      protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
      {
        return await base.SendAsync(request, cancellationToken);
      }
    }
  }
}
