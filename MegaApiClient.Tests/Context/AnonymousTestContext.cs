using System.Linq;
using Xunit;

namespace CG.Web.MegaApiClient.Tests.Context
{
  [CollectionDefinition("AnonymousLoginTests")]
  public class AnonymousLoginTestsCollection : ICollectionFixture<AnonymousTestContext> { }

  public class AnonymousTestContext : TestContext
  {
    public AnonymousTestContext()
    {
      this.ProtectedNodes = this.Client.GetNodes()
          .Where(x => x.Type == NodeType.Inbox || x.Type == NodeType.Root || x.Type == NodeType.Trash)
          .Select(x => x.Id)
          .ToArray();
      this.PermanentRootNodes = Enumerable.Empty<string>();
    }

    protected override void ConnectClient(IMegaApiClient client)
    {
      client.LoginAnonymous();
    }
  }
}
