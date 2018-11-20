using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CG.Web.MegaApiClient.Tests.Context;
using Xunit;
using Xunit.Abstractions;

namespace CG.Web.MegaApiClient.Tests
{
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
      var node = this.GetNode(((AuthenticatedTestContext)this.context).PermanentFilesNode);

      EventTester<double> eventTester = new EventTester<double>();
      IProgress<double> progress = new SyncProgress<double>(eventTester.OnRaised);

      string outputFile = Path.GetTempFileName();
      File.Delete(outputFile);

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

      string outputFile = Path.GetTempFileName();
      File.Delete(outputFile);

      // Act
      Task task = this.context.Client.DownloadFileAsync(new Uri(AuthenticatedTestContext.FileLink), outputFile, progress);
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

    private int Timeout
    {
      get
      {
        return (int)(this.context.WebTimeout * 0.9);
      }
    }
  }
}
