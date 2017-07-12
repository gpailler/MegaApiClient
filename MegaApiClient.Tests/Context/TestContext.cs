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

    private Lazy<IMegaApiClient> lazyClient;
    private readonly Lazy<IEnumerable<string>> lazyProtectedNodes;
    private readonly Lazy<IEnumerable<string>> lazyPermanentNodes;
    private ITestOutputHelper testOutputHelper;

    protected TestContext()
    {
      this.WebTimeout = 60000;
      this.lazyClient = new Lazy<IMegaApiClient>(this.InitializeClient);
      this.lazyProtectedNodes = new Lazy<IEnumerable<string>>(() => this.GetProtectedNodes().ToArray());
      this.lazyPermanentNodes = new Lazy<IEnumerable<string>>(() => this.GetPermanentNodes().ToArray());
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

    public void AssignLogger(ITestOutputHelper testOutputHelper)
    {
      this.testOutputHelper = testOutputHelper;
    }

    protected virtual IMegaApiClient CreateClient()
    {
      this.Options = new Options();
      this.WebClient = new TestWebClient(new WebClient(this.WebTimeout, null, new TestMessageHandler(this.testOutputHelper)), MaxRetry, this.testOutputHelper);

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

      this.testOutputHelper.WriteLine("Client created for context {0}", this.GetType().Name);

      return client;
    }

    private void OnApiRequestFailed(object sender, ApiRequestFailedEventArgs e)
    {
      this.testOutputHelper.WriteLine($"ApiRequestFailed: {e.ApiResult}, {e.ApiUrl}, {e.AttemptNum}, {e.DelayMilliseconds}ms, {e.ResponseJson}, {e.Exception} {e.Exception?.Message}");
    }

    private class TestMessageHandler : DelegatingHandler
    {
      private readonly ITestOutputHelper testOutputHelper;

      public TestMessageHandler(ITestOutputHelper testOutputHelper)
      {
        this.testOutputHelper = testOutputHelper;
        this.InnerHandler = new HttpClientHandler();
      }

      protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
      {
        this.testOutputHelper.WriteLine("Request: {0}", request);

        try
        {
          var response = await base.SendAsync(request, cancellationToken);
          this.testOutputHelper.WriteLine("Response: {0}", response);
          return response;
        }
        catch (Exception ex)
        {
          this.testOutputHelper.WriteLine("Exception: {0}-{1}", ex, ex.Message);
          throw;
        }
      }
    }
  }
}
