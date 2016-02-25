using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using NUnit.Framework;

namespace CG.Web.MegaApiClient.Tests
{
    [TestFixture]
    public class DownloadUploadAuthenticatedAsync : DownloadUploadAuthenticated
    {
        private const int Timeout = 20000;

        public DownloadUploadAuthenticatedAsync()
            : base(Options.AsyncWrapper)
        {
        }

        [TestCase(null, Result = 10L)]
        [TestCase(10L, Result = 65L)]
        public long DownloadFileAsync_FromNode_Succeeds(long? reportProgressChunkSize)
        {
            // Arrange
            long defaultReportProgressChunkSize = MegaApiClient.ReportProgressChunkSize;
            try
            {
                MegaApiClient.ReportProgressChunkSize =
                    reportProgressChunkSize.GetValueOrDefault(defaultReportProgressChunkSize);
                const string ExpectedFile = "Data/SampleFile.jpg";
                INode node = this.Client.GetNodes().Single(x => x.Id == this.PermanentFile);

                EventTester<double> eventTester = new EventTester<double>();
                Progress<double> progress = new Progress<double>(eventTester.OnRaised);

                string outputFile = Path.GetTempFileName();
                File.Delete(outputFile);

                // Act
                Task task = this.Client.DownloadFileAsync(node, outputFile, progress);
                bool result = task.Wait(Timeout);

                // Assert
                Assert.That(result, Is.True);
                this.AreFileEquivalent(ExpectedFile, outputFile);

                return eventTester.Calls;
            }
            finally
            {
                MegaApiClient.ReportProgressChunkSize = defaultReportProgressChunkSize;
            }
        }

        [TestCase("https://mega.co.nz/#!axYS1TLL!GJNtvGJXjdD1YZYqTj5SXQ8HtFvfocoSrtBSdbgeSLM", "Data/SampleFile.jpg")]
        public void DownloadFileAsync_FromLink_Succeeds(string uri, string expectedFile)
        {
            // Arrange
            EventTester<double> eventTester = new EventTester<double>();
            Progress<double> progress = new Progress<double>(eventTester.OnRaised);

            string outputFile = Path.GetTempFileName();
            File.Delete(outputFile);

            // Act
            Task task = this.Client.DownloadFileAsync(new Uri(uri), outputFile, progress);
            bool result = task.Wait(Timeout);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(eventTester.Calls, Is.EqualTo(10));
            this.AreFileEquivalent(expectedFile, outputFile);
        }

        [Test]
        public void UploadStreamAsync_DownloadLink_Succeeds()
        {
            //Arrange
            byte[] data = new byte[123456];
            this.random.NextBytes(data);

            INode parent = this.GetNode(NodeType.Root);

            using (Stream stream = new MemoryStream(data))
            {
                EventTester<double> eventTester = new EventTester<double>();
                Progress<double> progress = new Progress<double>(eventTester.OnRaised);

                // Act
                Task<INode> task = this.Client.UploadAsync(stream, "test", parent, progress);
                bool result = task.Wait(Timeout);

                // Assert
                Assert.That(result, Is.True);
                Assert.That(task.Result, Is.Not.Null);
                Assert.That(eventTester.Calls, Is.EqualTo(3));

                Uri uri = this.Client.GetDownloadLink(task.Result);
                stream.Position = 0;
                this.AreStreamsEquivalent(this.Client.Download(uri), stream);
            }
        }
    }
}
