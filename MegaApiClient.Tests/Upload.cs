using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Moq;
using NUnit.Framework;

namespace CG.Web.MegaApiClient.Tests
{
    [TestFixture]
    public class Upload : TestsBase
    {
        public Upload() : base(Options.LoginAuthenticated | Options.Clean)
        {
        }

        [TestCaseSource("GetInvalidUploadStreamParameters")]
        public void UploadStream_InvalidParameters_Throws(Stream stream, string name, INode parent)
        {
            Assert.That(
                () => this.Client.Upload(stream, name, parent),
                Throws.TypeOf<ArgumentNullException>());
        }

        [TestCase(NodeType.Root)]
        [TestCase(NodeType.Inbox)]
        [TestCase(NodeType.Trash)]
        public void UploadStream_Succeeds(NodeType parent)
        {
            byte[] data = new byte[123];
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
        }

        private IEnumerable<TestCaseData> GetInvalidUploadStreamParameters()
        {
            INode node = Mock.Of<INode>(x => x.Type == NodeType.Root);
            Stream stream = new MemoryStream();

            yield return new TestCaseData(null, null, null);
            yield return new TestCaseData(null, null, node);
            yield return new TestCaseData(null, "", null);
            yield return new TestCaseData(null, "", node);
            yield return new TestCaseData(null, "name", null);
            yield return new TestCaseData(null, "name", node);
            yield return new TestCaseData(stream, null, null);
            yield return new TestCaseData(stream, null, node);
            yield return new TestCaseData(stream, "", null);
            yield return new TestCaseData(stream, "", node);
            yield return new TestCaseData(stream, "name", null);
        }
    }
}
