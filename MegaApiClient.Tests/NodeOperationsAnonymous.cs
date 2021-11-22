using CG.Web.MegaApiClient.Tests.Context;
using Xunit;
using Xunit.Abstractions;

namespace CG.Web.MegaApiClient.Tests
{
  [CollectionDefinition(nameof(NodeOperationsAnonymous))]
  public class NodeOperationsAnonymousTestsCollection : ICollectionFixture<AnonymousTestContext> { }

  [Collection(nameof(NodeOperationsAnonymous))]
  public class NodeOperationsAnonymous : NodeOperations
  {
    public NodeOperationsAnonymous(AnonymousTestContext context, ITestOutputHelper testOutputHelper)
      : base(context, testOutputHelper)
    {
    }

    [Fact]
    public void GetAccountInformation_AnonymousUser_Succeeds()
    {
      var accountInformation = Context.Client.GetAccountInformation();

      Assert.NotNull(accountInformation);
      Assert.Equal(AuthenticatedTestContext.Inputs.TotalQuota, accountInformation.TotalQuota);
      Assert.Equal(0, accountInformation.UsedQuota);
    }
  }
}
