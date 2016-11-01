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
    [TestFixture]
    public class DownloadUploadAuthenticated : DownloadUpload
    {
        public DownloadUploadAuthenticated()
            : this(null)
        {
        }

        protected DownloadUploadAuthenticated(Options? options)
            : base(Options.LoginAuthenticated | Options.Clean | options.GetValueOrDefault())
        {
        }

        [Test]
        public void DownloadNode_ToStream_Succeeds()
        {
            const string ExpectedFile = "Data/SampleFile.jpg";
            INode node = this.Client.GetNodes().Single(x => x.Id == this.PermanentFile);

            using (Stream stream = this.Client.Download(node))
            {
                using (Stream expectedStream = new FileStream(this.GetAbsoluteFilePath(ExpectedFile), FileMode.Open))
                {
                    this.AreStreamsEquivalent(stream, expectedStream);
                }
            }
        }

        [TestCaseSource(typeof(DownloadUploadAuthenticated), nameof(GetGetDownloadLinkInvalidParameter))]
        public void GetDownloadLink_InvalidNode_Throws(INode node, IResolveConstraint constraint)
        {
            Assert.That(
                () => this.Client.GetDownloadLink(node),
                constraint);
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
                this.AreStreamsEquivalent(this.Client.Download(uri), stream);
            }
        }

        [TestCase("eooj3IwY", "https://mega.nz/#!ulISSQIb!RSz1DoCSGANrpphQtkr__uACIUZsFkiPWEkldOHNO20")]
        [TestCase("KoRAhTbQ", "https://mega.nz/#F!6kgE3YIQ!W_8GYHXH-COtmfWxOkMCFQ")]
        public void GetDownloadLink_ExistingLinks_Succeeds(string id, string expectedLink)
        {
            INode node = this.Client.GetNodes().Single(x => x.Id == id);

            var link = this.Client.GetDownloadLink(node);
            Assert.That(
                link.AbsoluteUri,
                Is.EqualTo(expectedLink));
        }

        [Test]
        public void GetDownloadLink_FolderNewLink_Succeeds()
        {
            // Create folders structure with subdirectories and file to ensure
            // SharedKey is distributed on all children
            var rootNode = this.GetNode(NodeType.Root);
            var folderNode = this.CreateFolderNode(rootNode, "Test");
            var subFolderNode = this.CreateFolderNode(folderNode, "AA");
            var subFolderNode2 = this.CreateFolderNode(folderNode, "BB");
            var subSubFolderNode = this.CreateFolderNode(subFolderNode, "subAA");
            var subSubFileNode = this.Client.UploadFile(this.GetAbsoluteFilePath("Data/SampleFile.jpg"), subSubFolderNode);

            Assert.DoesNotThrow(() => this.Client.GetDownloadLink(folderNode));

            var nodes = this.Client.GetNodes().ToArray();
            foreach (var node in new[] { folderNode, subFolderNode, subFolderNode2, subSubFolderNode, subSubFileNode })
            {
                var updatedNode = nodes.First(x => x.Id == node.Id);
                Assert.That(((INodeCrypto)updatedNode).SharedKey, Is.Not.Null);
            }
        }

        private static IEnumerable<ITestCaseData> GetGetDownloadLinkInvalidParameter()
        {
            yield return new TestCaseData(null, Throws.TypeOf<ArgumentNullException>());

            foreach (NodeType nodeType in new[] { NodeType.Inbox, NodeType.Root, NodeType.Trash })
            {
                Mock<INode> nodeMock = new Mock<INode>();
                nodeMock.SetupGet(x => x.Type).Returns(nodeType);
                yield return new TestCaseData(
                    nodeMock.Object,
                    Throws
                        .TypeOf<ArgumentException>()
                        .And.Message.EqualTo("Invalid node"));
            }

            Mock<INode> validTypeNodeMock = new Mock<INode>();
            validTypeNodeMock.SetupGet(x => x.Type).Returns(NodeType.File);
            yield return new TestCaseData(
                validTypeNodeMock.Object,
                Throws
                    .TypeOf<ArgumentException>()
                    .And.Message.EqualTo("node must implement INodeCrypto"));
        }
    }
}
