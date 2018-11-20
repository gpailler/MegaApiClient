namespace CG.Web.MegaApiClient.Tests
{
  using System;
  using Xunit;

  public class Options_Tests
  {
    [Fact]
    public void ReportProgressChunkSize_LowerThan_BufferSize_Throws()
    {
      var bufferSize = new Options().BufferSize;
      Assert.Throws<ArgumentException>("reportProgressChunkSize", () => new Options(reportProgressChunkSize: bufferSize - 1));
    }
  }
}
