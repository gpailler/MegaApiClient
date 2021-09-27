using System;
using System.Collections.Generic;
using System.Diagnostics;
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

    private readonly Lazy<IMegaApiClient> _lazyClient;
    private readonly Lazy<IEnumerable<string>> _lazyProtectedNodes;
    private readonly Lazy<IEnumerable<string>> _lazyPermanentNodes;
    private readonly Action<string> _logMessageAction;
    private ITestOutputHelper _testOutputHelper;

    protected TestContext()
    {
      WebTimeout = 60000;
      _lazyClient = new Lazy<IMegaApiClient>(InitializeClient);
      _lazyProtectedNodes = new Lazy<IEnumerable<string>>(() => GetProtectedNodes().ToArray());
      _lazyPermanentNodes = new Lazy<IEnumerable<string>>(() => GetPermanentNodes().ToArray());
      _logMessageAction = x =>
      {
        Debug.WriteLine(x);
        _testOutputHelper?.WriteLine(x);
      };
    }

    public IMegaApiClient Client => _lazyClient.Value;

    public IWebClient WebClient { get; private set; }

    public Options Options { get; private set; }

    public int WebTimeout { get; }

    public IEnumerable<string> ProtectedNodes => _lazyProtectedNodes.Value;

    public IEnumerable<string> PermanentRootNodes => _lazyPermanentNodes.Value;

    public void SetLogger(ITestOutputHelper testOutputHelper)
    {
      _testOutputHelper = testOutputHelper;
    }

    public void ClearLogger()
    {
      _testOutputHelper = null;
    }

    protected virtual IMegaApiClient CreateClient()
    {
      Options = new Options(applicationKey: "ewZQFBBC");
      WebClient = new TestWebClient(new WebClient(WebTimeout, null, new TestMessageHandler(), false), MaxRetry, _logMessageAction);

      return new MegaApiClient(Options, WebClient);
    }

    protected abstract IEnumerable<string> GetProtectedNodes();

    protected abstract IEnumerable<string> GetPermanentNodes();

    protected abstract void ConnectClient(IMegaApiClient client);

    private IMegaApiClient InitializeClient()
    {
      var client = CreateClient();
      client.ApiRequestFailed += OnApiRequestFailed;
      ConnectClient(client);

      _logMessageAction($"Client created for context {GetType().Name}");

      return client;
    }

    private void OnApiRequestFailed(object _, ApiRequestFailedEventArgs e)
    {
      _logMessageAction($"ApiRequestFailed: {e.ApiResult}, {e.ApiUrl}, {e.AttemptNum}, {e.RetryDelay}, {e.ResponseJson}, {e.Exception} {e.Exception?.Message}");
    }

    private class TestMessageHandler : HttpClientHandler
    {
      protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
      {
        return await base.SendAsync(request, cancellationToken);
      }
    }
  }
}
