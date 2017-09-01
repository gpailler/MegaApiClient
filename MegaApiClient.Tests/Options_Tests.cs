namespace CG.Web.MegaApiClient.Tests
{
  using System;
  using CG.Web.MegaApiClient.Tests.Context;
  using Xunit;
  using Xunit.Abstractions;

  [Collection("NotLoggedTests")]
  public class Options_Tests : TestsBase, IDisposable
  {
    public Options_Tests(NotLoggedTestContext context, ITestOutputHelper testOutputHelper)
      : base(context, testOutputHelper)
    {
    }

    public void Dispose()
    {
    }

    [Fact]
    public void ReportProgressChunkSize_LowerThan_BufferSize_Throws()
    {
      var bufferSize = new Options().BufferSize;
      Assert.Throws<ArgumentException>("reportProgressChunkSize", () => new Options(reportProgressChunkSize: bufferSize - 1));
    }
  }
}
