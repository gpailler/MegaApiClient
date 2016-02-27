using NUnit.Framework;

namespace CG.Web.MegaApiClient.Tests
{
    [TestFixture]
    public class NodeOperationsAnonymous : NodeOperations
    {
        public NodeOperationsAnonymous()
            : this(null)
        {
        }

        protected NodeOperationsAnonymous(Options? options)
            : base(Options.LoginAnonymous | options.GetValueOrDefault())
        {
        }
    }
}
