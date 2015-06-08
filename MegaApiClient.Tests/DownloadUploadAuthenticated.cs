using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Moq;
using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace CG.Web.MegaApiClient.Tests
{
    [TestFixture]
    public class DownloadUploadAuthenticated : DownloadUpload
    {
        public DownloadUploadAuthenticated()
            : base(Options.LoginAuthenticated | Options.Clean)
        {
        }

        [Test]
        public void DownloadNode_ToStream_Succeeds()
        {
            const string ExpectedFile = "Data/SampleFile.jpg";
            INode node = this.Client.GetNodes().Single(x => x.Id == this.PermanentFile);

            using (Stream stream = this.Client.Download(node))
            {
                using (Stream expectedStream = new FileStream(ExpectedFile, FileMode.Open))
                {
                    this.AreStreamsEquals(stream, expectedStream);
                }
            }
        }

        [TestCaseSource("GetGetDownloadLinkInvalidParameter")]
        public void GetDownloadLink_InvalidNode_Throws(INode node, IResolveConstraint constraint)
        {
            Assert.That(
                () => this.Client.GetDownloadLink(node),
                constraint);
        }

        [TestCase("https://mega.co.nz/#!axYS1TLL!GJNtvGJXjdD1YZYqTj5SXQ8HtFvfocoSrtBSdbgeSLM")]
        public void DownloadLink_Succeeds(string expectedLink)
        {
            INode node = this.Client.GetNodes().Single(x => x.Id == this.PermanentFile);

            Assert.That(
                this.Client.GetDownloadLink(node),
                Is.EqualTo(new Uri(expectedLink)));
        }

        private IEnumerable<ITestCaseData> GetGetDownloadLinkInvalidParameter()
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
