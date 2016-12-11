using System;
using Xunit;

namespace CG.Web.MegaApiClient.Tests.Context
{
  [CollectionDefinition("AnonymousLoginAsyncTests")]
  public class AnonymousLoginAsyncTestsCollection : ICollectionFixture<AnonymousAsyncTestContext> { }

  public class AnonymousAsyncTestContext : AnonymousTestContext, IDisposable
  {
    public void Dispose()
    {
      ((MegaApiClientAsyncWrapper)this.Client).Dispose();
    }

    protected override IMegaApiClient CreateClient()
    {
      return new MegaApiClientAsyncWrapper(base.CreateClient());
    }
  }
}
