using System.Collections.Generic;

namespace CG.Web.MegaApiClient.Tests.Context
{
  public abstract class TestContext : ITestContext
  {
    private const int WebTimeout = 30000;
    private const int MaxRetry = 3;

    protected TestContext()
    {
      this.Options = new Options();
      this.WebClient = new TestWebClient(new WebClient(WebTimeout), MaxRetry);
      this.Client = this.CreateClient();
    }

    public virtual IMegaApiClient Client { get; }

    public IWebClient WebClient { get; }

    public Options Options { get; }

    public IEnumerable<string> ProtectedNodes { get; protected set; }

    public IEnumerable<string> PermanentRootNodes { get; protected set; }

    protected virtual IMegaApiClient CreateClient()
    {
      return new MegaApiClient(this.Options, this.WebClient);
    }
  }
}
