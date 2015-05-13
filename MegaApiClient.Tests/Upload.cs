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
        public void UploadStream_Succeeds(NodeType parent)
        {
            byte[] data = new byte[200000]; // Data bigger than a single chunk
            Random r = new Random();
            r.NextBytes(data);

            INode root = this.GetNode(parent);

            INode node;
            using (Stream stream = new MemoryStream(data))
            {
                node = this.Client.Upload(stream, "test", root);
            }

            Assert.That(node, Is.Not.Null);
            Assert.That(node.Type, Is.EqualTo(NodeType.File));
            Assert.That(node.ParentId, Is.EqualTo(root.Id));
            Assert.That(node.Name, Is.EqualTo("test"));
            Assert.That(node.Size, Is.EqualTo(data.Length));
            Assert.That(node, Is.EqualTo(this.Client.GetNodes().Single(x => x.Id == node.Id)));

            Stream streamResult = this.Client.Download(node);
            byte[] dataResult = new byte[streamResult.Length];
            using (streamResult)
            {
                int read = streamResult.Read(dataResult, 0, dataResult.Length);
                Assert.That(read, Is.EqualTo(dataResult.Length));
            }

            Assert.That(dataResult, Is.EqualTo(data));
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
