using CG.Web.MegaApiClient.Tests.Context;
using Xunit;

namespace CG.Web.MegaApiClient.Tests
{
  [Collection("AnonymousLoginAsyncTests")]
  public class NodeOperationsAnonymousAsync : NodeOperations
  {
    public NodeOperationsAnonymousAsync(AnonymousAsyncTestContext context)
      : base(context)
    {
    }
  }
}
