using System;
using System.IO;
using System.Threading.Tasks;
using CG.Web.MegaApiClient.Tests.Context;
using Xunit;
using Xunit.Abstractions;

namespace CG.Web.MegaApiClient.Tests
{
  using xRetry;

  [Collection(nameof(AuthenticatedTestContext))]
  public class DownloadUploadAuthenticatedAsync : DownloadUploadAuthenticated
  {
    private readonly long _savedReportProgressChunkSize;

    public DownloadUploadAuthenticatedAsync(AuthenticatedAsyncTestContext context, ITestOutputHelper testOutputHelper)
      : base(context, testOutputHelper)
    {
      _savedReportProgressChunkSize = Context.Options.ReportProgressChunkSize;
    }

    public override void Dispose()
    {
      Context.Options.ReportProgressChunkSize = _savedReportProgressChunkSize;
      base.Dispose();
    }

    [Theory]
    [InlineData(null, 8)]
    [InlineData(1024L * 64L, 8)]
    [InlineData(100000L, 4)]
    [InlineData(200000L, 2)]
    public void DownloadFileAsync_FromNode_Succeeds(long? reportProgressChunkSize, long expectedResult)
    {
      // Arrange
      Context.Options.ReportProgressChunkSize = reportProgressChunkSize.GetValueOrDefault(Context.Options.ReportProgressChunkSize);
      var node = this.GetNode(AuthenticatedTestContext.Inputs.SharedFile.Id);

      var eventTester = new EventTester<double>();
      IProgress<double> progress = new SyncProgress<double>(eventTester.OnRaised);

      var outputFile = GetTempFileName();

      // Act
      Task task = Context.Client.DownloadFileAsync(node, outputFile, progress);
      var result = task.Wait(Timeout);

      // Assert
      Assert.True(result);
      AreFileEquivalent(GetAbsoluteFilePath("Data/SampleFile.jpg"), outputFile);

      Assert.Equal(expectedResult, eventTester.Calls);
    }

    [Fact]
    public void DownloadFileAsync_FromLink_Succeeds()
    {
      // Arrange
      const string ExpectedResultFile = "Data/SampleFile.jpg";

      var eventTester = new EventTester<double>();
      IProgress<double> progress = new SyncProgress<double>(eventTester.OnRaised);

      var outputFile = GetTempFileName();

      // Act
      var task = Context.Client.DownloadFileAsync(new Uri(AuthenticatedTestContext.Inputs.FileLink), outputFile, progress);
      var result = task.Wait(Timeout);

      // Assert
      Assert.True(result);
      Assert.Equal(8, eventTester.Calls);
      AreFileEquivalent(GetAbsoluteFilePath(ExpectedResultFile), outputFile);
    }

    [Theory]
    [InlineData(123456, 2)]
    public void UploadStreamAsync_DownloadLink_Succeeds(int dataSize, int expectedProgressionCalls)
    {
      //Arrange
      var data = new byte[dataSize];
      Random.NextBytes(data);

      var parent = GetNode(NodeType.Root);

      using Stream stream = new MemoryStream(data);
      double previousProgression = 0;
      var eventTester = new EventTester<double>();
      IProgress<double> progress = new SyncProgress<double>(x =>
      {
        if (previousProgression > x)
        {
          // Reset eventTester (because upload was restarted)
          eventTester.Reset();
        }

        previousProgression = x;
        eventTester.OnRaised(x);
      });

      // Act
      var task = Context.Client.UploadAsync(stream, "test", parent, progress);
      var result = task.Wait(Timeout);

      // Assert
      Assert.True(result);
      Assert.NotNull(task.Result);
      Assert.Equal(expectedProgressionCalls, eventTester.Calls);

      var uri = Context.Client.GetDownloadLink(task.Result);
      stream.Position = 0;
      AreStreamsEquivalent(Context.Client.Download(uri), stream);
    }

    [RetryFact]
    public void AsyncMethods_WithoutProgression_Succeeds()
    {
      var root = GetNode(NodeType.Root);
      var node = this.GetNode(AuthenticatedTestContext.Inputs.SharedFile.Id);
      var uri = new Uri(AuthenticatedTestContext.Inputs.FileLink);
      var sampleFilePath = GetAbsoluteFilePath("Data/SampleFile.jpg");
      var sampleFileStream = new FileStream(sampleFilePath, FileMode.Open, FileAccess.Read);

      var task1 = Context.Client.DownloadAsync(node);
      var result1 = task1.Wait(Timeout);
      Assert.True(result1);

      var task2 = Context.Client.DownloadAsync(uri);
      var result2 = task2.Wait(Timeout);
      Assert.True(result2);

      var outputFile3 = GetTempFileName();
      var task3 = Context.Client.DownloadFileAsync(node, outputFile3);
      var result3 = task3.Wait(Timeout);
      Assert.True(result3);

      var outputFile4 = GetTempFileName();
      var task4 = Context.Client.DownloadFileAsync(uri, outputFile4);
      var result4 = task4.Wait(Timeout);
      Assert.True(result4);

      var task5 = Context.Client.UploadAsync(sampleFileStream, Guid.NewGuid().ToString("N"), root);
      var result5 = task5.Wait(Timeout);
      Assert.True(result5);

      var task6 = Context.Client.UploadFileAsync(sampleFilePath, root);
      var result6 = task6.Wait(Timeout);
      Assert.True(result6);
    }

    private static int Timeout => (int)(TestContext.WebTimeout * 0.9);
  }
}
