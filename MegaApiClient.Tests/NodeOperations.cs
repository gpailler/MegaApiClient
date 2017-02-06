using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CG.Web.MegaApiClient.Tests.Context;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace CG.Web.MegaApiClient.Tests
{
  public abstract class NodeOperations : TestsBase
  {
    private const string DefaultNodeName = "NodeName";

    protected NodeOperations(ITestContext context, ITestOutputHelper testOutputHelper)
      : base(context, testOutputHelper)
    {
    }

    [Theory]
    [InlineData(NodeType.Root)]
    [InlineData(NodeType.Inbox)]
    [InlineData(NodeType.Trash)]
    public void Validate_DefaultNodes_Succeeds(NodeType nodeType)
    {
      // Arrange + Act
      var nodes = this.context.Client.GetNodes().ToArray();

      // Assert
      Assert.Equal(this.context.ProtectedNodes.Count(), nodes.Length);
      var node = Assert.Single(nodes, x => x.Type == nodeType);
      Assert.Empty(node.ParentId);
      Assert.Null(node.Name);
      Assert.Equal(0, node.Size);
    }

    [Theory]
    [InlineData(NodeType.Root)]
    [InlineData(NodeType.Inbox)]
    [InlineData(NodeType.Trash)]
    public void CreateFolder_Succeeds(NodeType parentNodeType)
    {
      // Arrange
      var parentNode = this.GetNode(parentNodeType);

      // Act
      var createdNode = this.CreateFolderNode(parentNode);

      // Assert
      Assert.NotNull(createdNode);
      Assert.Equal(DefaultNodeName, createdNode.Name);
      Assert.Equal(NodeType.Directory, createdNode.Type);
      Assert.Equal(0, createdNode.Size);
      Assert.NotNull(createdNode.ParentId);
      Assert.Equal(parentNode.Id, createdNode.ParentId);
      Assert.Single(this.context.Client.GetNodes(), x => x.Name == DefaultNodeName);
    }

    [Theory, MemberData(nameof(InvalidCreateFolderParameters))]
    public void CreateFolder_InvalidParameters_Throws(string name, INode parentNode, Type expectedExceptionType, string expectedMessage)
    {
      var exception = Assert.Throws(expectedExceptionType, () => this.context.Client.CreateFolder(name, parentNode));
      Assert.Equal(expectedMessage, exception.GetType() == typeof(ArgumentNullException) ? ((ArgumentNullException)exception).ParamName : exception.Message);
    }

    public static IEnumerable<object[]> InvalidCreateFolderParameters
    {
      get
      {
        yield return new object[] { null, null, typeof(ArgumentNullException), "name" };
        yield return new object[] { "", null, typeof(ArgumentNullException), "name" };
        yield return new object[] { "name", null, typeof(ArgumentNullException), "parent" };
        yield return new object[] { null, Mock.Of<INode>(x => x.Type == NodeType.File), typeof(ArgumentNullException), "name" };
        yield return new object[] { "name", Mock.Of<INode>(x => x.Type == NodeType.File), typeof(ArgumentException), "Invalid parent node" };
      }
    }

    [Theory]
    [InlineData("name", "name")]
    [InlineData("name", "NAME")]
    public void CreateFolder_SameName_Succeeds(string nodeName1, string nodeName2)
    {
      // Arrange
      var parentNode = this.GetNode(NodeType.Root);

      // Act
      var node1 = this.CreateFolderNode(parentNode, nodeName1);
      var node2 = this.CreateFolderNode(parentNode, nodeName2);

      // Assert
      Assert.NotEqual(node1, node2);
      Assert.NotSame(node1, node2);
    }

    [Fact]
    public void GetNodes_Succeeds()
    {
      // Arrange
      var parentNode = this.GetNode(NodeType.Root);

      // Act
      var createdNode = this.CreateFolderNode(parentNode);

      // Assert
      Assert.Equal(this.context.ProtectedNodes.Count() + 1, this.context.Client.GetNodes().Count());
      Assert.Single(this.context.Client.GetNodes(parentNode), x => x.Equals(createdNode));
    }

    [Fact]
    public void GetNodes_NullParentNode_Throws()
    {
      Assert.Throws<ArgumentNullException>("parent", () => this.context.Client.GetNodes(null));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(false)]
    [InlineData(true)]
    public void Delete_Succeeds(bool? moveToTrash)
    {
      // Arrange
      var parentNode = this.GetNode(NodeType.Root);
      var trashNode = this.GetNode(NodeType.Trash);
      var createdNode = this.CreateFolderNode(parentNode);

      // Assert
      var nodes = this.context.Client.GetNodes(parentNode).ToArray();
      Assert.Equal(this.context.PermanentRootNodes.Count() + 1, nodes.Length);
      Assert.Single(nodes, x => x.Equals(createdNode));
      Assert.Empty(this.context.Client.GetNodes(trashNode));

      // Act
      if (moveToTrash == null)
      {
        this.context.Client.Delete(createdNode);
      }
      else
      {

        this.context.Client.Delete(createdNode, moveToTrash.Value);
      }

      // Assert
      Assert.Equal(this.context.PermanentRootNodes.Count(), this.context.Client.GetNodes(parentNode).Count());

      if (moveToTrash.GetValueOrDefault(true))
      {
        Assert.Single(this.context.Client.GetNodes(trashNode), x => x.Equals(createdNode));
      }
      else
      {
        Assert.Empty(this.context.Client.GetNodes(trashNode));
      }
    }

    [Fact]
    public void Delete_NullNode_Throws()
    {
      Assert.Throws<ArgumentNullException>(() => this.context.Client.Delete(null));
    }

    [Theory]
    [InlineData(NodeType.Root)]
    [InlineData(NodeType.Inbox)]
    [InlineData(NodeType.Trash)]
    public void Delete_InvalidNode_Throws(NodeType nodeType)
    {
      var node = this.GetNode(nodeType);

      var exception = Assert.Throws<ArgumentException>(() => this.context.Client.Delete(node));
      Assert.Equal("Invalid node type", exception.Message);
    }

    [Theory]
    [InlineData(NodeType.Root)]
    [InlineData(NodeType.Trash)]
    [InlineData(NodeType.Inbox)]
    public void SameNode_Equality_Succeeds(NodeType nodeType)
    {
      var node1 = this.GetNode(nodeType);
      var node2 = this.GetNode(nodeType);

      Assert.Equal(node1, node2);
      Assert.Equal(node1.GetHashCode(), node2.GetHashCode());
      Assert.NotSame(node1, node2);
    }

    [Theory, MemberData(nameof(InvalidMoveParameters))]
    public void Move_InvalidParameters_Throws(INode node, INode destinationParentNode, Type expectedExceptionType, string expectedMessage)
    {
      var exception = Assert.Throws(expectedExceptionType, () => this.context.Client.Move(node, destinationParentNode));
      Assert.Equal(expectedMessage, exception.GetType() == typeof(ArgumentNullException) ? ((ArgumentNullException)exception).ParamName : exception.Message);
    }

    public static IEnumerable<object[]> InvalidMoveParameters
    {
      get
      {
        yield return new object[] { null, null, typeof(ArgumentNullException), "node" };
        yield return new object[] { null, Mock.Of<INode>(), typeof(ArgumentNullException), "node" };
        yield return new object[] { Mock.Of<INode>(), null, typeof(ArgumentNullException), "destinationParentNode" };
        yield return new object[] { Mock.Of<INode>(x => x.Type == NodeType.Root), Mock.Of<INode>(), typeof(ArgumentException), "Invalid node type" };
        yield return new object[] { Mock.Of<INode>(x => x.Type == NodeType.Inbox), Mock.Of<INode>(), typeof(ArgumentException), "Invalid node type" };
        yield return new object[] { Mock.Of<INode>(x => x.Type == NodeType.Trash), Mock.Of<INode>(), typeof(ArgumentException), "Invalid node type" };
        yield return new object[] { Mock.Of<INode>(x => x.Type == NodeType.File), Mock.Of<INode>(x => x.Type == NodeType.File), typeof(ArgumentException), "Invalid destination parent node" };
      }
    }

    [Theory]
    [InlineData(NodeType.Inbox)]
    [InlineData(NodeType.Trash)]
    public void Move_Succeeds(NodeType destinationParentNodeType)
    {
      // Arrange
      var parentNode = this.GetNode(NodeType.Root);
      var destinationParentNode = this.GetNode(destinationParentNodeType);
      var node = this.CreateFolderNode(parentNode);

      // Assert
      Assert.Single(this.context.Client.GetNodes(parentNode), x => x.Equals(node));
      Assert.Empty(this.context.Client.GetNodes(destinationParentNode));

      // Act
      var movedNode = this.context.Client.Move(node, destinationParentNode);

      // Assert
      Assert.Empty(this.context.Client.GetNodes(parentNode).Where(x => x.Equals(node)));
      Assert.Single(this.context.Client.GetNodes(destinationParentNode), x => x.Equals(movedNode));
    }

    [Theory]
    [InlineData(NodeType.Directory)]
    [InlineData(NodeType.File)]
    public void Rename_Succeeds(NodeType nodeType)
    {
      // Arrange
      var parentNode = this.GetNode(NodeType.Root);
      INode createdNode;
      DateTime modificationDate = new DateTime(2000, 01, 02, 03, 04, 05);
      switch (nodeType)
      {
        case NodeType.Directory:
          createdNode = this.context.Client.CreateFolder("Data", parentNode);
          break;

        case NodeType.File:
          byte[] data = new byte[123];
          new Random().NextBytes(data);

          using (MemoryStream stream = new MemoryStream(data))
          {
            createdNode = this.context.Client.Upload(stream, "Data", parentNode, modificationDate);
          }
          break;

        default:
          throw new NotSupportedException();
      }

      // Assert
      var nodes = this.context.Client.GetNodes(parentNode).ToArray();
      Assert.Equal(this.context.PermanentRootNodes.Count() + 1, nodes.Length);
      Assert.Single(nodes, x => x.Equals(createdNode));
      if (nodeType == NodeType.File)
      {
        Assert.Equal(modificationDate, createdNode.ModificationDate);
      }

      // Act
      var renamedNode = this.context.Client.Rename(createdNode, "Data2");

      // Assert
      Assert.Equal("Data2", renamedNode.Name);
      nodes = this.context.Client.GetNodes(parentNode).ToArray();
      Assert.Equal(this.context.PermanentRootNodes.Count() + 1, nodes.Length);
      Assert.Single(nodes, x => x.Equals(createdNode));
      if (nodeType == NodeType.File)
      {
        Assert.Equal(modificationDate, renamedNode.ModificationDate);
      }
    }

    [Fact]
    public void GetNodeFromLink_Browse_Succeeds()
    {
      const string link = "https://mega.nz/#!ulISSQIb!RSz1DoCSGANrpphQtkr__uACIUZsFkiPWEkldOHNO20";
      var node = this.context.Client.GetNodeFromLink(new Uri(link));

      Assert.NotNull(node);
      Assert.Equal("SharedFile.jpg", node.Name);
      Assert.Equal(523265, node.Size);
      Assert.Equal(DateTime.Parse("2015-07-14T14:04:51.0000000+08:00"), node.ModificationDate);
    }

    [Fact]
    public void GetNodesFromLink_Succeeds()
    {
      const string folderLink = "https://mega.nz/#F!6kgE3YIQ!W_8GYHXH-COtmfWxOkMCFQ";
      var nodes = this.context.Client.GetNodesFromLink(new Uri(folderLink));

      Assert.Equal(3, nodes.Count());
      INode node;
      node = Assert.Single(nodes, x => x.Name == "SharedFile.jpg");
      Assert.Equal(523265, node.Size);
      Assert.Equal(DateTime.Parse("2015-07-14T14:04:51.0000000+08:00"), node.ModificationDate);
      Assert.Equal(DateTime.Parse("2016-04-15T16:42:56.0000000+02:00"), node.CreationDate);

      node = Assert.Single(nodes, x => x.Name == "SharedFolder");
      Assert.Equal(0, node.Size);
      Assert.Equal(DateTime.Parse("2016-04-15 22:41:35.0000000+08:00"), node.CreationDate);
      Assert.Equal(null, node.ModificationDate);

      node = Assert.Single(nodes, x => x.Name == "SharedSubFolder");
      Assert.Equal(0, node.Size);
      Assert.Equal(DateTime.Parse("2016-04-15 22:41:48.0000000+08:00"), node.CreationDate);
      Assert.Equal(null, node.ModificationDate);
    }
  }
}