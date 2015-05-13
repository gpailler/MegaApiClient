using NUnit.Framework;

namespace CG.Web.MegaApiClient.Tests
{
    [TestFixture]
    public class NodeOperationsAuthenticated : NodeOperations
    {
        public NodeOperationsAuthenticated()
            : base(Options.LoginAuthenticated | Options.Clean)
        {
        }
    }
}
