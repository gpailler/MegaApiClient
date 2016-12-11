using CG.Web.MegaApiClient.Tests.Context;
using Xunit;

namespace CG.Web.MegaApiClient.Tests
{
  [Collection("AnonymousLoginTests")]
  public class NodeOperationsAnonymous : NodeOperations
  {
    public NodeOperationsAnonymous(AnonymousTestContext context)
      : base(context)
    {
    }
  }
}
