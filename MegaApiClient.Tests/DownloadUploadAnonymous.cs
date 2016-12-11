using CG.Web.MegaApiClient.Tests.Context;
using Xunit;

namespace CG.Web.MegaApiClient.Tests
{
  [Collection("AnonymousLoginTests")]
  public class DownloadUploadAnonymous : DownloadUpload
  {
    public DownloadUploadAnonymous(AnonymousTestContext context)
      : base(context)
    {
    }
  }
}
