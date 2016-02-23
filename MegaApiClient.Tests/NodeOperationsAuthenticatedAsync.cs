using NUnit.Framework;

namespace CG.Web.MegaApiClient.Tests
{
    [TestFixture]
    public class NodeOperationsAuthenticatedAsync : NodeOperationsAuthenticated
    {
        public NodeOperationsAuthenticatedAsync()
            : base(Options.AsyncWrapper)
        {
        }
    }
}
