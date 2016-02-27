using NUnit.Framework;

namespace CG.Web.MegaApiClient.Tests
{
    [TestFixture]
    public class NodeOperationsAnonymousAsync : NodeOperationsAnonymous
    {
        public NodeOperationsAnonymousAsync()
            : base(Options.AsyncWrapper)
        {
        }
    }
}
