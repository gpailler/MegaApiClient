using NUnit.Framework;

namespace CG.Web.MegaApiClient.Tests
{
    [TestFixture]
    public class UploadAuthenticated : Upload
    {
        public UploadAuthenticated()
            : base(Options.LoginAuthenticated | Options.Clean)
        {
        }
    }
}
