using CG.Web.MegaApiClient.Tests.Context;
using Xunit;

namespace CG.Web.MegaApiClient.Tests
{
  [Collection("AuthenticatedLoginAsyncTests")]
  public class NodeOperationsAuthenticatedAsync : NodeOperationsAuthenticated
  {
    public NodeOperationsAuthenticatedAsync(AuthenticatedAsyncTestContext context)
      : base(context)
    {
    }
  }
}
