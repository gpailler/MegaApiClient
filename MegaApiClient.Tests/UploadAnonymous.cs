using NUnit.Framework;

namespace CG.Web.MegaApiClient.Tests
{
    [TestFixture]
    public class UploadAnonymous : Upload
    {
        public UploadAnonymous()
            : base(Options.LoginAnonymous)
        {
        }
    }
}
