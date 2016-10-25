using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using NUnit.Framework.Interfaces;

namespace CG.Web.MegaApiClient.Tests
{
    public abstract class NodeOperations : TestsBase
    {
        private const string DefaultNodeName = "NodeName";

        protected NodeOperations(Options options)
            : base(options)
        {
        }

        [TestCase(NodeType.Root)]
        [TestCase(NodeType.Inbox)]
        [TestCase(NodeType.Trash)]
        public void Validate_DefaultNodes_Succeeds(NodeType nodeType)
        {
            var nodes = this.Client.GetNodes().ToArray();

            Assert.That(nodes, Has.Length.EqualTo(this.SystemNodesCount + this.PermanentNodesCount));
            Assert.That(nodes, Has.Exactly(1)
                .Matches<INode>(x => x.Type == nodeType)
                .And.Property<INode>(x => x.ParentId).Empty
                .And.Property<INode>(x => x.Name).Null
                .And.Property<INode>(x => x.Size).EqualTo(0));
        }

        [TestCase(NodeType.Root)]
        [TestCase(NodeType.Inbox)]
        [TestCase(NodeType.Trash)]
        public void CreateFolder_Succeeds(NodeType parentNodeType)
        {
            var parentNode = this.GetNode(parentNodeType);
            var createdNode = this.CreateFolderNode(parentNode);

            Assert.That(createdNode, Is.Not.Null
                .And.Property<INode>(x => x.Name).EqualTo(DefaultNodeName)
                .And.Property<INode>(x => x.Type).EqualTo(NodeType.Directory)
                .And.Property<INode>(x => x.Size).EqualTo(0)
                .And.Property<INode>(x => x.ParentId).Not.Null
                .And.Property<INode>(x => x.ParentId).EqualTo(parentNode.Id));

            var nodes = this.Client.GetNodes();
            Assert.That(nodes, Has.Exactly(1)
                .Matches<INode>(x => x.Name == DefaultNodeName));
        }

        [TestCaseSource(typeof(NodeOperations), nameof(GetInvalidCreateFolderParameters))]
        public void CreateFolder_InvalidParameters_Throws(string name, INode parentNode, IResolveConstraint constraint)
        {
            Assert.That(
                () => this.Client.CreateFolder(name, parentNode),
                constraint);
        }

        [TestCase("name", "name")]
        [TestCase("name", "NAME")]
        public void CreateFolder_SameName_Succeeds(string nodeName1, string nodeName2)
        {
            var parentNode = this.GetNode(NodeType.Root);
            var node1 = this.CreateFolderNode(parentNode, name: nodeName1);

            INode node2 = null;
            Assert.That(
                () => node2 = this.CreateFolderNode(parentNode, name: nodeName2),
                Throws.Nothing);

            Assert.That(node1, Is.Not.EqualTo(node2));
        }

        [Test]
        public void GetNodes_Succeeds()
        {
            var parentNode = this.GetNode(NodeType.Root);
            var createdNode = this.CreateFolderNode(parentNode);

            Assert.That(
                this.Client.GetNodes().ToArray(),
                Has.Length.EqualTo(this.SystemNodesCount + this.PermanentNodesCount + 1));

            Assert.That(
                this.Client.GetNodes(parentNode).ToArray(), 
                Has.Length.EqualTo(this.PermanentFoldersRootNodesCount + 1)
                .And.Exactly(1).EqualTo(createdNode));
        }

        [Test]
        public void GetNodes_NullParentNode_Throws()
        {
            Assert.That(
                () => this.Client.GetNodes(null),
                Throws.TypeOf<ArgumentNullException>()
                .With.Property<ArgumentNullException>(x => x.ParamName).EqualTo("parent"));
        }

        [TestCase(null)]
        [TestCase(false)]
        [TestCase(true)]
        public void Delete_Succeeds(bool? moveToTrash)
        {
            var parentNode = this.GetNode(NodeType.Root);
            var trashNode = this.GetNode(NodeType.Trash);
            var createdNode = this.CreateFolderNode(parentNode);

            Assert.That(
                this.Client.GetNodes(parentNode).ToArray(),
                Has.Length.EqualTo(this.PermanentFoldersRootNodesCount + 1)
                .And.Exactly(1).EqualTo(createdNode));

            Assert.That(
                this.Client.GetNodes(trashNode),
                Is.Empty);

            if (moveToTrash == null)
            {
                this.Client.Delete(createdNode);
            }
            else
            {

                this.Client.Delete(createdNode, moveToTrash.Value);
            }

            Assert.That(
                this.Client.GetNodes(parentNode).ToArray(),
                Has.Length.EqualTo(this.PermanentFoldersRootNodesCount));

            if (moveToTrash.GetValueOrDefault(true))
            {
                Assert.That(
                    this.Client.GetNodes(trashNode).ToArray(),
                    Has.Length.EqualTo(1)
                    .And.Exactly(1).EqualTo(createdNode));
            }
            else
            {
                Assert.That(
                    this.Client.GetNodes(trashNode),
                    Is.Empty);
            }
        }

        [Test]
        public void Delete_NullNode_Throws()
        {
            Assert.That(
                () => this.Client.Delete(null),
                Throws.TypeOf<ArgumentNullException>());
        }

        [TestCase(NodeType.Root)]
        [TestCase(NodeType.Inbox)]
        [TestCase(NodeType.Trash)]
        public void Delete_InvalidNode_Throws(NodeType nodeType)
        {
            var node = this.GetNode(nodeType);

            Assert.That(
                () => this.Client.Delete(node),
                Throws.TypeOf<ArgumentException>()
                .And.Message.EqualTo("Invalid node type"));
        }

        [TestCase(NodeType.Root)]
        [TestCase(NodeType.Trash)]
        [TestCase(NodeType.Inbox)]
        public void SameNode_Equality_Succeeds(NodeType nodeType)
        {
            var node1 = this.Client.GetNodes().First(x => x.Type == nodeType);
            var node2 = this.Client.GetNodes().First(x => x.Type == nodeType);
            
            Assert.That(node1,
                Is.EqualTo(node2)
                .And.Matches<INode>(x => x.GetHashCode() == node2.GetHashCode())
                .And.Not.SameAs(node2));
        }

        [TestCaseSource(typeof(NodeOperations), nameof(GetInvalidMoveParameters))]
        public void Move_InvalidParameters_Throws(INode node, INode destinationParentNode, IResolveConstraint constraint)
        {
            Assert.That(
                () => this.Client.Move(node, destinationParentNode),
                constraint);
        }

        [TestCase(NodeType.Root, IgnoreReason = "Cannot move on itself")]
        [TestCase(NodeType.Inbox)]
        [TestCase(NodeType.Trash)]
        public void Move_Succeeds(NodeType destinationParentNodeType)
        {
            var parentNode = this.GetNode(NodeType.Root);
            var destinationParentNode = this.GetNode(destinationParentNodeType);
            var node = this.CreateFolderNode(parentNode);

            Assert.That(
                this.Client.GetNodes(parentNode),
                Has.Exactly(1).EqualTo(node));

            Assert.That(
                this.Client.GetNodes(destinationParentNode),
                Is.Empty);

            var movedNode = this.Client.Move(node, destinationParentNode);

            Assert.That(
                this.Client.GetNodes(parentNode),
                Has.Exactly(0).EqualTo(node));

            Assert.That(
                this.Client.GetNodes(destinationParentNode),
                Has.Exactly(1).EqualTo(movedNode));
        }

        [TestCase("https://mega.nz/#!38JjRYIA!RSz1DoCSGANrpphQtkr__uACIUZsFkiPWEkldOHNO20")]
        public void GetNodeFromLink_Succeeds(string link)
        {
            INodePublic publicNode = this.Client.GetNodeFromLink(new Uri(link));

            Assert.That(publicNode, Is.Not.Null
                .And.Property<INodePublic>(x => x.Name).EqualTo("SharedFile.jpg")
                .And.Property<INodePublic>(x => x.Size).EqualTo(523265));
        }

        private static IEnumerable<ITestCaseData> GetInvalidCreateFolderParameters()
        {
            yield return new TestCaseData(null, null, 
                Throws.TypeOf<ArgumentNullException>()
                .With.Property<ArgumentNullException>(x => x.ParamName).EqualTo("name"));

            yield return new TestCaseData("", null,
                Throws.TypeOf<ArgumentNullException>()
                .With.Property<ArgumentNullException>(x => x.ParamName).EqualTo("name"));

            yield return new TestCaseData("name", null,
                Throws.TypeOf<ArgumentNullException>()
                .With.Property<ArgumentNullException>(x => x.ParamName).EqualTo("parent"));

            yield return new TestCaseData(null, Mock.Of<INode>(x => x.Type == NodeType.File),
                Throws.TypeOf<ArgumentNullException>()
                .With.Property<ArgumentNullException>(x => x.ParamName).EqualTo("name"));

            yield return new TestCaseData("name", Mock.Of<INode>(x => x.Type == NodeType.File),
                Throws.TypeOf<ArgumentException>()
                .With.Message.EqualTo("Invalid parent node"));
        }

        private static IEnumerable<ITestCaseData> GetInvalidMoveParameters()
        {
            yield return new TestCaseData(null, null, 
                Throws.TypeOf<ArgumentNullException>()
                .With.Property<ArgumentNullException>(x => x.ParamName).EqualTo("node"));

            yield return new TestCaseData(null, Mock.Of<INode>(),
                Throws.TypeOf<ArgumentNullException>()
                .With.Property<ArgumentNullException>(x => x.ParamName).EqualTo("node"));

            yield return new TestCaseData(Mock.Of<INode>(), null,
                Throws.TypeOf<ArgumentNullException>()
                .With.Property<ArgumentNullException>(x => x.ParamName).EqualTo("destinationParentNode"));

            yield return new TestCaseData(Mock.Of<INode>(x => x.Type == NodeType.Root), Mock.Of<INode>(),
                Throws.TypeOf<ArgumentException>()
                    .With.Message.EqualTo("Invalid node type"));

            yield return new TestCaseData(Mock.Of<INode>(x => x.Type == NodeType.Inbox), Mock.Of<INode>(),
                Throws.TypeOf<ArgumentException>()
                    .With.Message.EqualTo("Invalid node type"));

            yield return new TestCaseData(Mock.Of<INode>(x => x.Type == NodeType.Trash), Mock.Of<INode>(),
                Throws.TypeOf<ArgumentException>()
                    .With.Message.EqualTo("Invalid node type"));

            yield return new TestCaseData(Mock.Of<INode>(x => x.Type == NodeType.File), Mock.Of<INode>(x => x.Type == NodeType.File),
                Throws.TypeOf<ArgumentException>()
                .With.Message.EqualTo("Invalid destination parent node"));
        }
    }
}