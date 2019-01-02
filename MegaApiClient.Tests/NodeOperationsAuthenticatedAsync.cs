using CG.Web.MegaApiClient.Tests.Context;
using Xunit;
using Xunit.Abstractions;

namespace CG.Web.MegaApiClient.Tests
{
  [Collection(nameof(AuthenticatedTestContext))]
  public class NodeOperationsAuthenticatedAsync : NodeOperationsAuthenticated
  {
    public NodeOperationsAuthenticatedAsync(AuthenticatedAsyncTestContext context, ITestOutputHelper testOutputHelper)
      : base(context, testOutputHelper)
    {
    }
  }
}
