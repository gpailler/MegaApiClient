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
        [TestCase("u95ATDYA", "SsRDGA4Y", NodeType.Directory, "SharedSubFolder", 0, "2015-07-14T18:24:31.0000000+08:00")]
        [TestCase("KshlkSIK", "SsRDGA4Y", NodeType.File, "SharedFile.jpg", 523265, "2015-05-19T09:40:47.0000000+08:00")]
        [TestCase("b0I0QDhA", "u4IgDb5K", NodeType.Directory, "SharedRemoteFolder", 0, "2015-05-21T02:35:22.0000000+08:00")]
        [TestCase("e5wjkSJB", "b0I0QDhA", NodeType.File, "SharedRemoteFile.jpg", 523265, "2015-05-21T02:36:06.0000000+08:00")]
        [TestCase("KhZSWI7C", "b0I0QDhA", NodeType.Directory, "SharedRemoteSubFolder", 0, "2015-07-14T17:05:03.0000000+08:00")]
        [TestCase("HtonzYYY", "KhZSWI7C", NodeType.File, "SharedRemoteSubFile.jpg", 523265, "2015-07-14T18:06:27.0000000+08:00")]
        [TestCase("z1YCibCT", "KhZSWI7C", NodeType.Directory, "SharedRemoteSubSubFolder", 0, "2015-07-14T18:01:56.0000000+08:00")]
        public void Validate_PermanentNodes_Succeeds(
            string id,
            string expectedParent,
            NodeType expectedNodeType,
            string expectedName,
            long expectedSize,
            string expectedModificationDate
            )
        {
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
