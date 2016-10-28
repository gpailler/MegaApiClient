using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Moq;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using NUnit.Framework.Interfaces;

namespace CG.Web.MegaApiClient.Tests
{
    public abstract class DownloadUpload : TestsBase
    {
        protected readonly Random random = new Random();

        protected DownloadUpload(Options options)
            : base(options)
        {
        }

        [TestCaseSource(typeof(DownloadUpload), nameof(GetInvalidUploadStreamParameters))]
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
                this.AreStreamsEquivalent(this.Client.Download(node), stream);
            }
        }

        [TestCaseSource(typeof(DownloadUpload), nameof(GetDownloadLinkInvalidParameter))]
        public void DownloadLink_ToStream_InvalidParameter_Throws(Uri uri, IResolveConstraint constraint)
        {
            Assert.That(
                () => this.Client.Download(uri),
                constraint);
        }

        [TestCase("https://mega.nz/#!2sZwQJRZ!RSz1DoCSGANrpphQtkr__uACIUZsFkiPWEkldOHNO20", "Data/SampleFile.jpg")]
        public void DownloadLink_ToStream_Succeeds(string link, string expectedResultFile)
        {
            using (Stream stream = new FileStream(this.GetAbsoluteFilePath(expectedResultFile), FileMode.Open))
            {
                this.AreStreamsEquivalent(this.Client.Download(new Uri(link)), stream);
            }
        }

        [TestCase("https://mega.nz/#!2sZwQJRZ!RSz1DoCSGANrpphQtkr__uACIUZsFkiPWEkldOHNO20")]
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

        [TestCaseSource(typeof(DownloadUpload), nameof(GetDownloadLinkToFileInvalidParameter))]
        public void DownloadLink_ToFile_InvalidParameter_Throws(Uri uri, string outFile, IResolveConstraint constraint)
        {
            Assert.That(
                () => this.Client.DownloadFile(uri, outFile),
                constraint);
        }

        [TestCase("https://mega.nz/#!2sZwQJRZ!RSz1DoCSGANrpphQtkr__uACIUZsFkiPWEkldOHNO20", "Data/SampleFile.jpg")]
        public void DownloadLink_ToFile_Succeeds(string link, string expectedResultFile)
        {
            string outFile = Path.GetTempFileName();
            File.Delete(outFile);
            this.Client.DownloadFile(new Uri(link), outFile);

            Assert.That(
                File.ReadAllBytes(outFile),
                Is.EqualTo(File.ReadAllBytes(this.GetAbsoluteFilePath(expectedResultFile))));
        }

        protected void AreStreamsEquivalent(Stream stream1, Stream stream2)
        {
            byte[] stream1data = new byte[stream1.Length];
            byte[] stream2data = new byte[stream2.Length];

            int readStream1 = stream1.Read(stream1data, 0, stream1data.Length);
            Assert.That(readStream1, Is.EqualTo(stream1data.Length));

            int readStream2 = stream2.Read(stream2data, 0, stream2data.Length);
            Assert.That(readStream2, Is.EqualTo(stream2data.Length));

            Assert.That(stream1data, Is.EqualTo(stream2data));
        }

        protected void AreFileEquivalent(string file1, string file2)
        {
            using (Stream stream1 = new FileStream(file1, FileMode.Open))
            {
                using (Stream stream2 = new FileStream(file2, FileMode.Open))
                {
                    this.AreStreamsEquivalent(stream1, stream2);
                }
            }
        }

        private static IEnumerable<TestCaseData> GetInvalidUploadStreamParameters()
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

        private static IEnumerable<ITestCaseData> GetDownloadLinkInvalidParameter()
        {
            yield return new TestCaseData(null, Throws.TypeOf<ArgumentNullException>());
            yield return new TestCaseData(new Uri("http://www.example.com"), Throws.TypeOf<ArgumentException>());
            yield return new TestCaseData(new Uri("https://mega.nz"), Throws.TypeOf<ArgumentException>());
            yield return new TestCaseData(new Uri("https://mega.nz/#!axYS1TLL"), Throws.TypeOf<ArgumentException>());
            yield return new TestCaseData(new Uri("https://mega.nz/#!axYS1TLL!"), Throws.TypeOf<ArgumentException>());
        }

        private static IEnumerable<ITestCaseData> GetDownloadLinkToFileInvalidParameter()
        {
            string outFile = Path.GetTempFileName();

            yield return new TestCaseData(null, null, Throws.TypeOf<ArgumentNullException>());
            yield return new TestCaseData(null, outFile, Throws.TypeOf<ArgumentNullException>());
            yield return new TestCaseData(new Uri("http://www.example.com"), outFile, Throws.TypeOf<ArgumentException>());
            yield return new TestCaseData(new Uri("https://mega.nz"), outFile, Throws.TypeOf<ArgumentException>());
            yield return new TestCaseData(new Uri("https://mega.nz/#!38JjRYIA"), outFile, Throws.TypeOf<ArgumentException>());
            yield return new TestCaseData(new Uri("https://mega.nz/#!2sZwQJRZ!"), outFile, Throws.TypeOf<ArgumentException>());
            yield return new TestCaseData(new Uri("https://mega.nz/#!2sZwQJRZ!RSz1DoCSGANrpphQtkr__uACIUZsFkiPWEkldOHNO20"), null, Throws.TypeOf<ArgumentNullException>());
            yield return new TestCaseData(new Uri("https://mega.nz/#!2sZwQJRZ!RSz1DoCSGANrpphQtkr__uACIUZsFkiPWEkldOHNO20"), string.Empty, Throws.TypeOf<ArgumentNullException>());


            yield return new TestCaseData(new Uri("https://mega.co.nz/#!2sZwQJRZ!RSz1DoCSGANrpphQtkr__uACIUZsFkiPWEkldOHNO20"), outFile, Throws.TypeOf<IOException>());
        }
    }
}
