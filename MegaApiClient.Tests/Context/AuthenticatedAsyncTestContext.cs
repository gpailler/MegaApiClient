using Xunit;

namespace CG.Web.MegaApiClient.Tests.Context
{
  [CollectionDefinition("AuthenticatedLoginAsyncTests")]
  public class AuthenticatedLoginAsyncTestsCollection : ICollectionFixture<AuthenticatedAsyncTestContext> { }

  public class AuthenticatedAsyncTestContext : AuthenticatedTestContext
  {
    public override void Dispose()
    {
      ((MegaApiClientAsyncWrapper)this.Client).Dispose();
      base.Dispose();
    }

    protected override IMegaApiClient CreateClient()
    {
      return new MegaApiClientAsyncWrapper(base.CreateClient());
    }
  }
}
