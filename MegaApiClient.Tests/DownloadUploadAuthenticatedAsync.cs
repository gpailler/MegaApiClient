using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CG.Web.MegaApiClient.Tests.Context;
using Xunit;
using Xunit.Abstractions;

namespace CG.Web.MegaApiClient.Tests
{
  [Collection("AuthenticatedLoginAsyncTests")]
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
    [InlineData(null, 10L)]
    [InlineData(10L, 65L)]
    public long DownloadFileAsync_FromNode_Succeeds(long? reportProgressChunkSize, long expectedResult)
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

      return eventTester.Calls;
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
      Assert.Equal(10, eventTester.Calls);
      this.AreFileEquivalent(this.GetAbsoluteFilePath(expectedResultFile), outputFile);
    }

    [Fact]
    public void UploadStreamAsync_DownloadLink_Succeeds()
    {
      //Arrange
      byte[] data = new byte[123456];
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
        Assert.Equal(3, eventTester.Calls);

        Uri uri = this.context.Client.GetDownloadLink(task.Result);
        stream.Position = 0;
        this.AreStreamsEquivalent(this.context.Client.Download(uri), stream);
      }
    }

    private int Timeout
    {
      get
      {
        return this.context.WebTimeout - 10000;
      }
    }
  }
}
