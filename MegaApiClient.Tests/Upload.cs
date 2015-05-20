using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Moq;
using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace CG.Web.MegaApiClient.Tests
{
    public abstract class Upload : TestsBase
    {
        private readonly Random random = new Random();

        protected Upload(Options options)
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

        [TestCase(20000)] // 1 chunk
        [TestCase(200000)] // 2 chunks
        [TestCase(2000000)] // 3 chunks
        public void UploadStream_ValidateContent_Succeeds(int dataSize)
        {
            byte[] uploadedData = new byte[dataSize];
            this.random.NextBytes(uploadedData);

            INode parent = this.GetNode(NodeType.Root);

            INode node;
            using (Stream stream = new MemoryStream(uploadedData))
            {
                node = this.Client.Upload(stream, "test", parent);
            }

            byte[] downloadedData;
            using (Stream streamResult = this.Client.Download(node))
            { 
                downloadedData = new byte[streamResult.Length];
                int read = streamResult.Read(downloadedData, 0, downloadedData.Length);
                Assert.That(read, Is.EqualTo(downloadedData.Length));
            }

            Assert.That(downloadedData, Is.EqualTo(uploadedData));
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
    }
}
