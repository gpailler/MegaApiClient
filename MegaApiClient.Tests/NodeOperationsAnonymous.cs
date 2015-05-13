using NUnit.Framework;

namespace CG.Web.MegaApiClient.Tests
{
    [TestFixture]
    public class NodeOperationsAnonymous : NodeOperations
    {
        public NodeOperationsAnonymous()
            : base(Options.LoginAnonymous)
        {
        }
    }
}
