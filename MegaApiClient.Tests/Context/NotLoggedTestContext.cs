using Xunit;

namespace CG.Web.MegaApiClient.Tests.Context
{

  [CollectionDefinition("NotLoggedTests")]
  public class NotLoggedTestsCollection : ICollectionFixture<NotLoggedTestContext> { }

  public class NotLoggedTestContext : TestContext
  {
  }
}
