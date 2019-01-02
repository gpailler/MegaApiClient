using CG.Web.MegaApiClient.Tests.Context;
using Xunit;
using Xunit.Abstractions;

namespace CG.Web.MegaApiClient.Tests
{
  [Collection(nameof(AnonymousAsyncTestContext))]
  public class NodeOperationsAnonymousAsync : NodeOperations
  {
    public NodeOperationsAnonymousAsync(AnonymousAsyncTestContext context, ITestOutputHelper testOutputHelper)
      : base(context, testOutputHelper)
    {
    }
  }
}
