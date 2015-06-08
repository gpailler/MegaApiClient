using NUnit.Framework;

namespace CG.Web.MegaApiClient.Tests
{
    [TestFixture]
    public class DownloadUploadAnonymous : DownloadUpload
    {
        public DownloadUploadAnonymous()
            : base(Options.LoginAnonymous)
        {
        }
    }
}
