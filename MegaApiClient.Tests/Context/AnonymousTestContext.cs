using System.Collections.Generic;
using System.Linq;

namespace CG.Web.MegaApiClient.Tests.Context
{
  public class AnonymousTestContext : TestContext
  {
    protected override void ConnectClient(IMegaApiClient client)
    {
      client.LoginAnonymous();
    }

    protected override IEnumerable<string> GetProtectedNodes()
    {
      return Client.GetNodes()
          .Where(x => x.Type == NodeType.Inbox || x.Type == NodeType.Root || x.Type == NodeType.Trash)
          .Select(x => x.Id);
    }

    protected override IEnumerable<string> GetPermanentNodes()
    {
      return Enumerable.Empty<string>();
    }
  }
}
