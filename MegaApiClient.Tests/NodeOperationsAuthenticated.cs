using System;
using System.Linq;
using NUnit.Framework;

namespace CG.Web.MegaApiClient.Tests
{
    [TestFixture]
    public class NodeOperationsAuthenticated : NodeOperations
    {
        public NodeOperationsAuthenticated()
            : base(Options.LoginAuthenticated | Options.Clean)
        {
        }

        [TestCase("SsRDGA4Y", "bsxVBKLL", NodeType.Directory, "SharedFolder", 0, "2015-05-19T09:38:13.0000000+08:00")]
        [TestCase("KshlkSIK", "SsRDGA4Y", NodeType.File, "SharedFile.jpg", 523265, "2015-05-19T09:40:47.0000000+08:00")]
        public void Validate_PermanentNodes_Succeeds(
            string id,
            string expectedParent,
            NodeType expectedNodeType,
            string expectedName,
            long expectedSize,
            string expectedModificationDate
            )
        {
            var parentNode = this.GetNode(NodeType.Root);
            var node = this.Client.GetNodes().SingleOrDefault(x => x.Id == id);

            Assert.That(node, Is.Not.Null
                .And.Property<INode>(x => x.Type).EqualTo(expectedNodeType)
                .And.Property<INode>(x => x.ParentId).EqualTo(expectedParent)
                .And.Property<INode>(x => x.Name).EqualTo(expectedName)
                .And.Property<INode>(x => x.Size).EqualTo(expectedSize)
                .And.Property<INode>(x => x.LastModificationDate).EqualTo(DateTime.Parse(expectedModificationDate)));
        }
    }
}
