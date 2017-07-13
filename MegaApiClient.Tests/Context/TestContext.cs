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
      this.Options = new Options(applicationKey: "ewZQFBBC");
      this.WebClient = new TestWebClient(new WebClient(this.WebTimeout, null, new TestMessageHandler(this.testOutputHelper)), MaxRetry, this.testOutputHelper);

      // Add some delay before any call
      Random r = new Random();
      ((TestWebClient)this.WebClient).OnCalled += (arg1, arg2) => Thread.Sleep((int)r.Next(250, 750));

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

    private class TestMessageHandler : HttpClientHandler
    {
      private readonly ITestOutputHelper testOutputHelper;

      public TestMessageHandler(ITestOutputHelper testOutputHelper)
      {
        this.testOutputHelper = testOutputHelper;
      }

      protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
      {
        try
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
        catch (InvalidOperationException e)
        {
          Console.WriteLine("Call outside a test: {0}", e.Message);
          return await base.SendAsync(request, cancellationToken);
        }
      }
    }
  }
}
