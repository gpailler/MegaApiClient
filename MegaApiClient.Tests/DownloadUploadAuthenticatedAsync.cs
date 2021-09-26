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
    private readonly long savedReportProgressChunkSize;

    public DownloadUploadAuthenticatedAsync(AuthenticatedAsyncTestContext context, ITestOutputHelper testOutputHelper)
      : base(context, testOutputHelper)
    {
      this.savedReportProgressChunkSize = this.context.Options.ReportProgressChunkSize;
    }

    public override void Dispose()
    {
      this.context.Options.ReportProgressChunkSize = this.savedReportProgressChunkSize;
      base.Dispose();
    }

    [Theory]
    [InlineData(null, 8)]
    [InlineData(1024 * 64, 8)]
    [InlineData(100000, 4)]
    [InlineData(200000, 2)]
    public void DownloadFileAsync_FromNode_Succeeds(long? reportProgressChunkSize, long expectedResult)
    {
      // Arrange
      this.context.Options.ReportProgressChunkSize = reportProgressChunkSize.GetValueOrDefault(this.context.Options.ReportProgressChunkSize);
      var node = this.GetNode(AuthenticatedTestContext.Inputs.SharedFile.Id);

      EventTester<double> eventTester = new EventTester<double>();
      IProgress<double> progress = new SyncProgress<double>(eventTester.OnRaised);

      string outputFile = this.GetTempFileName();

      // Act
      Task task = this.context.Client.DownloadFileAsync(node, outputFile, progress);
      bool result = task.Wait(this.Timeout);

      // Assert
      Assert.True(result);
      this.AreFileEquivalent(this.GetAbsoluteFilePath("Data/SampleFile.jpg"), outputFile);

      Assert.Equal(expectedResult, eventTester.Calls);
    }

    [Fact]
    public void DownloadFileAsync_FromLink_Succeeds()
    {
      // Arrange
      const string expectedResultFile = "Data/SampleFile.jpg";

      EventTester<double> eventTester = new EventTester<double>();
      IProgress<double> progress = new SyncProgress<double>(eventTester.OnRaised);

      string outputFile = this.GetTempFileName();

      // Act
      Task task = this.context.Client.DownloadFileAsync(new Uri(AuthenticatedTestContext.Inputs.FileLink), outputFile, progress);
      bool result = task.Wait(this.Timeout);

      // Assert
      Assert.True(result);
      Assert.Equal(8, eventTester.Calls);
      this.AreFileEquivalent(this.GetAbsoluteFilePath(expectedResultFile), outputFile);
    }

    [Theory]
    [InlineData(123456, 2)]
    public void UploadStreamAsync_DownloadLink_Succeeds(int dataSize, int expectedProgressionCalls)
    {
      //Arrange
      byte[] data = new byte[dataSize];
      this.random.NextBytes(data);

      INode parent = this.GetNode(NodeType.Root);

      using (Stream stream = new MemoryStream(data))
      {
        double previousProgression = 0;
        EventTester<double> eventTester = new EventTester<double>();
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
        Task<INode> task = this.context.Client.UploadAsync(stream, "test", parent, progress);
        bool result = task.Wait(this.Timeout);

        // Assert
        Assert.True(result);
        Assert.NotNull(task.Result);
        Assert.Equal(expectedProgressionCalls, eventTester.Calls);

        Uri uri = this.context.Client.GetDownloadLink(task.Result);
        stream.Position = 0;
        this.AreStreamsEquivalent(this.context.Client.Download(uri), stream);
      }
    }

    [RetryFact]
    public void AsyncMethods_WithoutProgression_Succeeds()
    {
      var root = this.GetNode(NodeType.Root);
      var node = this.GetNode(AuthenticatedTestContext.Inputs.SharedFile.Id);
      var uri = new Uri(AuthenticatedTestContext.Inputs.FileLink);
      var sampleFilePath = this.GetAbsoluteFilePath("Data/SampleFile.jpg");
      var sampleFileStream = new FileStream(sampleFilePath, FileMode.Open, FileAccess.Read);

      var task1 = this.context.Client.DownloadAsync(node);
      var result1 = task1.Wait(this.Timeout);
      Assert.True(result1);

      var task2 = this.context.Client.DownloadAsync(uri);
      var result2 = task2.Wait(this.Timeout);
      Assert.True(result2);

      var outputFile3 = this.GetTempFileName();
      var task3 = this.context.Client.DownloadFileAsync(node, outputFile3);
      var result3 = task3.Wait(this.Timeout);
      Assert.True(result3);

      var outputFile4 = this.GetTempFileName();
      var task4 = this.context.Client.DownloadFileAsync(uri, outputFile4);
      var result4 = task4.Wait(this.Timeout);
      Assert.True(result4);

      var task5 = this.context.Client.UploadAsync(sampleFileStream, Guid.NewGuid().ToString("N"), root);
      var result5 = task5.Wait(this.Timeout);
      Assert.True(result5);

      var task6 = this.context.Client.UploadFileAsync(sampleFilePath, root);
      var result6 = task6.Wait(this.Timeout);
      Assert.True(result6);
    }

    private int Timeout
    {
      get
      {
        return (int)(this.context.WebTimeout * 0.9);
      }
    }
  }
}
