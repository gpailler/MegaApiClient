using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Moq;
using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace CG.Web.MegaApiClient.Tests
{
    public abstract class DownloadUpload : TestsBase
    {
        private readonly Random random = new Random();

        protected DownloadUpload(Options options)
            : base(options)
        {
        }

        [TestCaseSource("GetInvalidUploadStreamParameters")]
        public void UploadStream_InvalidParameters_Throws(Stream stream, string name, INode parent, IResolveConstraint constraint)
        {
            Assert.That(
                () => this.Client.Upload(stream, name, parent),
                constraint);
        }

        [TestCase(NodeType.Root)]
        [TestCase(NodeType.Inbox)]
        [TestCase(NodeType.Trash)]
        public void UploadStream_DifferentParent_Succeeds(NodeType parentType)
        {
            byte[] data = new byte[123];
            this.random.NextBytes(data);

            INode parent = this.GetNode(parentType);

            INode node;
            using (Stream stream = new MemoryStream(data))
            {
                node = this.Client.Upload(stream, "test", parent);
            }

            Assert.That(node, Is.Not.Null);
            Assert.That(node.Type, Is.EqualTo(NodeType.File));
            Assert.That(node.ParentId, Is.EqualTo(parent.Id));
            Assert.That(node.Name, Is.EqualTo("test"));
            Assert.That(node.Size, Is.EqualTo(data.Length));
            Assert.That(node, Is.EqualTo(this.Client.GetNodes().Single(x => x.Id == node.Id)));
        }
        
        [Test]
        public void UploadStream_DownloadLink_Succeeds()
        {
            byte[] data = new byte[123];
            this.random.NextBytes(data);

            INode parent = this.GetNode(NodeType.Root);

            INode node;
            using (Stream stream = new MemoryStream(data))
            {
                node = this.Client.Upload(stream, "test", parent);

                Uri uri = this.Client.GetDownloadLink(node);

                stream.Position = 0;
                this.AreStreamsEquals(this.Client.Download(uri), stream);
            }
        }

        [TestCase(20000)] // 1 chunk
        [TestCase(200000)] // 2 chunks
        [TestCase(2000000)] // 3 chunks
        public void UploadStream_ValidateContent_Succeeds(int dataSize)
        {
            byte[] uploadedData = new byte[dataSize];
            this.random.NextBytes(uploadedData);

            INode parent = this.GetNode(NodeType.Root);

            using (Stream stream = new MemoryStream(uploadedData))
            {
                var node = this.Client.Upload(stream, "test", parent);

                stream.Position = 0;
                this.AreStreamsEquals(this.Client.Download(node), stream);
            }
        }

        [TestCaseSource("GetDownloadLinkInvalidParameter")]
        public void DownloadLink_ToStream_InvalidParameter_Throws(Uri uri, IResolveConstraint constraint)
        {
            Assert.That(
                () => this.Client.Download(uri),
                constraint);
        }

        [TestCase("https://mega.co.nz/#!axYS1TLL!GJNtvGJXjdD1YZYqTj5SXQ8HtFvfocoSrtBSdbgeSLM", "Data/SampleFile.jpg")]
        public void DownloadLink_ToStream_Succeeds(string link, string expectedResultFile)
        {
            using (Stream stream = new FileStream(expectedResultFile, FileMode.Open))
            {
                this.AreStreamsEquals(this.Client.Download(new Uri(link)), stream);
            }
        }

        [TestCase("https://mega.co.nz/#!axYS1TLL!GJNtvGJXjdD1YZYqTj5SXQ8HtFvfocoSrtBSdbgeSLM")]
        public void Download_ValidateStream_Succeeds(string link)
        {
            using (Stream stream = this.Client.Download(new Uri(link)))
            {
                Assert.That(
                    stream,
                    Is.Not.Null
                    .And.Property<Stream>(x => x.Length).EqualTo(523265)
                    .And.Property<Stream>(x => x.CanRead).True
                    .And.Property<Stream>(x => x.CanSeek).False
                    .And.Property<Stream>(x => x.CanTimeout).False
                    .And.Property<Stream>(x => x.CanWrite).False
                    .And.Property<Stream>(x => x.Position).EqualTo(0)
                    );
            }
        }

        [TestCaseSource("GetDownloadLinkToFileInvalidParameter")]
        public void DownloadLink_ToFile_InvalidParameter_Throws(Uri uri, string outFile, IResolveConstraint constraint)
        {
            Assert.That(
                () => this.Client.DownloadFile(uri, outFile),
                constraint);
        }

        [TestCase("https://mega.co.nz/#!axYS1TLL!GJNtvGJXjdD1YZYqTj5SXQ8HtFvfocoSrtBSdbgeSLM", "Data/SampleFile.jpg")]
        public void DownloadLink_ToFile_Succeeds(string link, string expectedResultFile)
        {
            string outFile = Path.GetTempFileName();
            File.Delete(outFile);
            this.Client.DownloadFile(new Uri(link), outFile);

            Assert.That(
                File.ReadAllBytes(outFile),
                Is.EqualTo(File.ReadAllBytes(expectedResultFile)));
        }

        protected IEnumerable<TestCaseData> GetInvalidUploadStreamParameters()
        {
            INode nodeDirectory = Mock.Of<INode>(x => x.Type == NodeType.Directory);
            INode nodeFile = Mock.Of<INode>(x => x.Type == NodeType.File);
            Stream stream = new MemoryStream();

            yield return new TestCaseData(null, null, null, Throws.TypeOf<ArgumentNullException>());
            yield return new TestCaseData(null, null, nodeDirectory, Throws.TypeOf<ArgumentNullException>());
            yield return new TestCaseData(null, "", null, Throws.TypeOf<ArgumentNullException>());
            yield return new TestCaseData(null, "", nodeDirectory, Throws.TypeOf<ArgumentNullException>());
            yield return new TestCaseData(null, "name", null, Throws.TypeOf<ArgumentNullException>());
            yield return new TestCaseData(null, "name", nodeDirectory, Throws.TypeOf<ArgumentNullException>());
            yield return new TestCaseData(stream, null, null, Throws.TypeOf<ArgumentNullException>());
            yield return new TestCaseData(stream, null, nodeDirectory, Throws.TypeOf<ArgumentNullException>());
            yield return new TestCaseData(stream, "", null, Throws.TypeOf<ArgumentNullException>());
            yield return new TestCaseData(stream, "", nodeDirectory, Throws.TypeOf<ArgumentNullException>());
            yield return new TestCaseData(stream, "name", null, Throws.TypeOf<ArgumentNullException>());
            yield return new TestCaseData(stream, "name", nodeFile, Throws.TypeOf<ArgumentException>());
        }

        protected IEnumerable<ITestCaseData> GetDownloadLinkInvalidParameter()
        {
            yield return new TestCaseData(null, Throws.TypeOf<ArgumentNullException>());
            yield return new TestCaseData(new Uri("http://www.example.com"), Throws.TypeOf<ArgumentException>());
            yield return new TestCaseData(new Uri("https://mega.co.nz"), Throws.TypeOf<ArgumentException>());
            yield return new TestCaseData(new Uri("https://mega.co.nz/#!axYS1TLL"), Throws.TypeOf<ArgumentException>());
            yield return new TestCaseData(new Uri("https://mega.co.nz/#!axYS1TLL!"), Throws.TypeOf<ArgumentException>());
        }

        protected IEnumerable<ITestCaseData> GetDownloadLinkToFileInvalidParameter()
        {
            string outFile = Path.GetTempFileName();

            yield return new TestCaseData(null, null, Throws.TypeOf<ArgumentNullException>());
            yield return new TestCaseData(null, outFile, Throws.TypeOf<ArgumentNullException>());
            yield return new TestCaseData(new Uri("http://www.example.com"), outFile, Throws.TypeOf<ArgumentException>());
            yield return new TestCaseData(new Uri("https://mega.co.nz"), outFile, Throws.TypeOf<ArgumentException>());
            yield return new TestCaseData(new Uri("https://mega.co.nz/#!axYS1TLL"), outFile, Throws.TypeOf<ArgumentException>());
            yield return new TestCaseData(new Uri("https://mega.co.nz/#!axYS1TLL!"), outFile, Throws.TypeOf<ArgumentException>());
            yield return new TestCaseData(new Uri("https://mega.co.nz/#!axYS1TLL!GJNtvGJXjdD1YZYqTj5SXQ8HtFvfocoSrtBSdbgeSLM"), null, Throws.TypeOf<ArgumentNullException>());
            yield return new TestCaseData(new Uri("https://mega.co.nz/#!axYS1TLL!GJNtvGJXjdD1YZYqTj5SXQ8HtFvfocoSrtBSdbgeSLM"), string.Empty, Throws.TypeOf<ArgumentNullException>());

            
            yield return new TestCaseData(new Uri("https://mega.co.nz/#!axYS1TLL!GJNtvGJXjdD1YZYqTj5SXQ8HtFvfocoSrtBSdbgeSLM"), outFile, Throws.TypeOf<IOException>());
        }

        protected void AreStreamsEquals(Stream stream1, Stream stream2)
        {
            byte[] stream1data = new byte[stream1.Length];
            byte[] stream2data = new byte[stream2.Length];

            int readStream1 = stream1.Read(stream1data, 0, stream1data.Length);
            Assert.That(readStream1, Is.EqualTo(stream1data.Length));

            int readStream2 = stream2.Read(stream2data, 0, stream2data.Length);
            Assert.That(readStream2, Is.EqualTo(stream2data.Length));

            Assert.That(stream1data, Is.EqualTo(stream2data));
        }
    }
}
