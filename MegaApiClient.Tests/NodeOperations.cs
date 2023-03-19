using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CG.Web.MegaApiClient.Serialization;
using CG.Web.MegaApiClient.Tests.Context;
using Moq;
using Newtonsoft.Json;
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
      var nodes = Context.Client.GetNodes().ToArray();

      // Assert
      Assert.Equal(Context.ProtectedNodes.Count(), nodes.Length);
      var node = Assert.Single(nodes, x => x.Type == nodeType);
      Assert.Empty(node.ParentId);
      Assert.Null(node.Name);
      Assert.Equal(0, node.Size);
    }

    [Theory]
    [InlineData(NodeType.Root)]
    [InlineData(NodeType.Trash)]
    public void CreateFolder_Succeeds(NodeType parentNodeType)
    {
      // Arrange
      var parentNode = GetNode(parentNodeType);

      // Act
      var createdNode = CreateFolderNode(parentNode);

      // Assert
      Assert.NotNull(createdNode);
      Assert.Equal(DefaultNodeName, createdNode.Name);
      Assert.Equal(NodeType.Directory, createdNode.Type);
      Assert.Equal(0, createdNode.Size);
      Assert.NotNull(createdNode.ParentId);
      Assert.Equal(parentNode.Id, createdNode.ParentId);
      Assert.Single(Context.Client.GetNodes(), x => x.Name == DefaultNodeName);
    }

    [Theory, MemberData(nameof(InvalidCreateFolderParameters))]
    public void CreateFolder_InvalidParameters_Throws(string name, NodeType? parentNodeType, Type expectedExceptionType, string expectedMessage)
    {
      var parentNode = parentNodeType == null ? null : Mock.Of<INode>(x => x.Type == parentNodeType.Value);
      var exception = Assert.Throws(expectedExceptionType, () => Context.Client.CreateFolder(name, parentNode));
      Assert.Equal(expectedMessage, exception.GetType() == typeof(ArgumentNullException) ? ((ArgumentNullException)exception).ParamName : exception.Message);
    }

    public static IEnumerable<object[]> InvalidCreateFolderParameters
    {
      get
      {
        yield return new object[] { null, null, typeof(ArgumentNullException), "name" };
        yield return new object[] { "", null, typeof(ArgumentNullException), "name" };
        yield return new object[] { "name", null, typeof(ArgumentNullException), "parent" };
        yield return new object[] { null, NodeType.File, typeof(ArgumentNullException), "name" };
        yield return new object[] { "name", NodeType.File, typeof(ArgumentException), "Invalid parent node" };
      }
    }

    [Theory]
    [InlineData("name", "name")]
    [InlineData("name", "NAME")]
    public void CreateFolder_SameName_Succeeds(string nodeName1, string nodeName2)
    {
      // Arrange
      var parentNode = GetNode(NodeType.Root);

      // Act
      var node1 = CreateFolderNode(parentNode, nodeName1);
      var node2 = CreateFolderNode(parentNode, nodeName2);

      // Assert
      Assert.NotEqual(node1, node2);
      Assert.NotSame(node1, node2);
    }

    [Fact]
    public void GetNodes_Succeeds()
    {
      // Arrange
      var parentNode = GetNode(NodeType.Root);

      // Act
      var createdNode = CreateFolderNode(parentNode);

      // Assert
      Assert.Equal(Context.ProtectedNodes.Count() + 1, Context.Client.GetNodes().Count());
      Assert.Single(Context.Client.GetNodes(parentNode), x => x.Equals(createdNode));
    }

    [Fact]
    public void GetNodes_NullParentNode_Throws()
    {
      Assert.Throws<ArgumentNullException>("parent", () => Context.Client.GetNodes(null));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(false)]
    [InlineData(true)]
    public void Delete_Succeeds(bool? moveToTrash)
    {
      // Arrange
      var parentNode = GetNode(NodeType.Root);
      var trashNode = GetNode(NodeType.Trash);
      var createdNode = CreateFolderNode(parentNode);

      // Assert
      var nodes = Context.Client.GetNodes(parentNode).ToArray();
      Assert.Equal(Context.PermanentRootNodes.Count() + 1, nodes.Length);
      Assert.Single(nodes, x => x.Equals(createdNode));
      Assert.Empty(Context.Client.GetNodes(trashNode));

      // Act
      if (moveToTrash == null)
      {
        Context.Client.Delete(createdNode);
      }
      else
      {

        Context.Client.Delete(createdNode, moveToTrash.Value);
      }

      // Assert
      Assert.Equal(Context.PermanentRootNodes.Count(), Context.Client.GetNodes(parentNode).Count());

      if (moveToTrash.GetValueOrDefault(true))
      {
        Assert.Single(Context.Client.GetNodes(trashNode), x => x.Equals(createdNode));
      }
      else
      {
        Assert.Empty(Context.Client.GetNodes(trashNode));
      }
    }

    [Fact]
    public void Delete_NullNode_Throws()
    {
      Assert.Throws<ArgumentNullException>(() => Context.Client.Delete(null));
    }

    [Theory]
    [InlineData(NodeType.Root)]
    [InlineData(NodeType.Inbox)]
    [InlineData(NodeType.Trash)]
    public void Delete_InvalidNode_Throws(NodeType nodeType)
    {
      var node = GetNode(nodeType);

      var exception = Assert.Throws<ArgumentException>(() => Context.Client.Delete(node));
      Assert.Equal("Invalid node type", exception.Message);
    }

    [Theory]
    [InlineData(NodeType.Root)]
    [InlineData(NodeType.Trash)]
    [InlineData(NodeType.Inbox)]
    public void SameNode_Equality_Succeeds(NodeType nodeType)
    {
      var node1 = GetNode(nodeType);
      var node2 = GetNode(nodeType);

      Assert.Equal(node1, node2);
      Assert.Equal(node1.GetHashCode(), node2.GetHashCode());
      Assert.NotSame(node1, node2);
    }

    [Theory, MemberData(nameof(InvalidMoveParameters))]
    public void Move_InvalidParameters_Throws(NodeType? nodeType, NodeType? destinationParentNodeType, Type expectedExceptionType, string expectedMessage)
    {
      var node = nodeType == null ? null : Mock.Of<INode>(x => x.Type == nodeType.Value);
      var destinationParentNode = destinationParentNodeType == null ? null : Mock.Of<INode>(x => x.Type == destinationParentNodeType.Value);
      var exception = Assert.Throws(expectedExceptionType, () => Context.Client.Move(node, destinationParentNode));
      Assert.Equal(expectedMessage, exception.GetType() == typeof(ArgumentNullException) ? ((ArgumentNullException)exception).ParamName : exception.Message);
    }

    public static IEnumerable<object[]> InvalidMoveParameters
    {
      get
      {
        yield return new object[] { null, null, typeof(ArgumentNullException), "node" };
        yield return new object[] { null, NodeType.Directory, typeof(ArgumentNullException), "node" };
        yield return new object[] { NodeType.File, null, typeof(ArgumentNullException), "destinationParentNode" };
        yield return new object[] { NodeType.Root, NodeType.Directory, typeof(ArgumentException), "Invalid node type" };
        yield return new object[] { NodeType.Inbox, NodeType.Directory, typeof(ArgumentException), "Invalid node type" };
        yield return new object[] { NodeType.Trash, NodeType.Directory, typeof(ArgumentException), "Invalid node type" };
        yield return new object[] { NodeType.File, NodeType.File, typeof(ArgumentException), "Invalid destination parent node" };
      }
    }

    [Theory]
    [InlineData(NodeType.Trash)]
    public void Move_Succeeds(NodeType destinationParentNodeType)
    {
      // Arrange
      var parentNode = GetNode(NodeType.Root);
      var destinationParentNode = GetNode(destinationParentNodeType);
      var node = CreateFolderNode(parentNode);

      // Assert
      Assert.Single(Context.Client.GetNodes(parentNode), x => x.Equals(node));
      Assert.Empty(Context.Client.GetNodes(destinationParentNode));

      // Act
      var movedNode = Context.Client.Move(node, destinationParentNode);

      // Assert
      Assert.Empty(Context.Client.GetNodes(parentNode).Where(x => x.Equals(node)));
      Assert.Single(Context.Client.GetNodes(destinationParentNode), x => x.Equals(movedNode));
    }

    [Theory]
    [InlineData(NodeType.Directory)]
    [InlineData(NodeType.File)]
    public void Rename_Succeeds(NodeType nodeType)
    {
      // Arrange
      var parentNode = GetNode(NodeType.Root);
      INode createdNode;
      var modificationDate = new DateTime(2000, 01, 02, 03, 04, 05);
      switch (nodeType)
      {
        case NodeType.Directory:
          createdNode = Context.Client.CreateFolder("Data", parentNode);
          break;

        case NodeType.File:
          var data = new byte[123];
          new Random().NextBytes(data);

          using (var stream = new MemoryStream(data))
          {
            createdNode = Context.Client.Upload(stream, "Data", parentNode, modificationDate);
          }

          break;

        default:
          throw new NotSupportedException();
      }

      // Assert
      var nodes = Context.Client.GetNodes(parentNode).ToArray();
      Assert.Equal(Context.PermanentRootNodes.Count() + 1, nodes.Length);
      Assert.Single(nodes, x => x.Equals(createdNode));
      if (nodeType == NodeType.File)
      {
        Assert.Equal(modificationDate, createdNode.ModificationDate);
      }

      // Act
      var renamedNode = Context.Client.Rename(createdNode, "Data2");

      // Assert
      Assert.Equal("Data2", renamedNode.Name);
      nodes = Context.Client.GetNodes(parentNode).ToArray();
      Assert.Equal(Context.PermanentRootNodes.Count() + 1, nodes.Length);
      Assert.Single(nodes, x => x.Equals(createdNode));
      if (nodeType == NodeType.File)
      {
        Assert.Equal(modificationDate, renamedNode.ModificationDate);
      }
    }

    [Theory]
    [JsonInputsDataAttribute("FileLink")]
    public void GetNodeFromLink_WithFileAttributes_Succeeds(string fileLink)
    {
      var node = Context.Client.GetNodeFromLink(new Uri(fileLink));

      Assert.NotNull(node);
      Assert.Equal("SharedFile.jpg", node.Name);
      Assert.Equal(AuthenticatedTestContext.Inputs.SharedFile.Size, node.Size);
      Assert.Equal(AuthenticatedTestContext.Inputs.SharedFile.ModificationDate, node.ModificationDate);
      Assert.Null(node.CreationDate);
      Assert.Equal(AuthenticatedTestContext.Inputs.SharedFile.Fingerprint, node.Fingerprint);
      Assert.Equal(2, node.FileAttributes.Length);
    }

    [Theory]
    [JsonInputsDataAttribute("FolderLink")]
    public void GetNodeFromLink_WithFolderLink_Throws(string folderLink)
    {
      var exception = Assert.Throws<ArgumentException>(() => Context.Client.GetNodeFromLink(new Uri(folderLink)));
      Assert.Equal("Uri must be a valid file share starting with /file/. Use GetNodesFromLink() for folder share (Parameter 'uri')", exception.Message);
    }

    [Theory]
    [JsonInputsDataAttribute("FileLink")]
    public void GetNodesFromLink_WithFileLink_Throws(string fileLink)
    {
      var exception = Assert.Throws<ArgumentException>(() => Context.Client.GetNodesFromLink(new Uri(fileLink)));
      Assert.Equal("Uri must be a valid folder share starting with /folder/. Use GetNodeFromLink() for file share (Parameter 'uri')", exception.Message);
    }

    [Theory]
    [JsonInputsDataAttribute("ZipFileLink")]
    public void GetNodeFromLink_WithoutFileAttributes_Succeeds(string fileLink)
    {
      var node = Context.Client.GetNodeFromLink(new Uri(fileLink));

      Assert.NotNull(node);
      Assert.Equal("SampleZipFile.zip", node.Name);
      Assert.Equal(AuthenticatedTestContext.Inputs.SampleZipFile.Size, node.Size);
      Assert.Equal(AuthenticatedTestContext.Inputs.SampleZipFile.ModificationDate, node.ModificationDate);
      Assert.Null(node.CreationDate);
      Assert.Equal(AuthenticatedTestContext.Inputs.SampleZipFile.Fingerprint, node.Fingerprint);
      Assert.Empty(node.FileAttributes);
    }

    [Theory]
    [JsonInputsDataAttribute(new object[] { null }, new string[] { "FolderLink" })]
    [JsonInputsDataAttribute(new object[] { "/file/SELECTED_FILE_NODE_ID" }, new string[] { "FolderLink" })]
    [JsonInputsDataAttribute(new object[] { "/folder/SELECTED_FOLDER_NODE_ID" }, new string[] { "FolderLink" })]
    public void GetNodesFromLink_Succeeds(string suffix, string folderLink)
    {
      var nodes = Context.Client.GetNodesFromLink(new Uri(folderLink + suffix));

      Assert.Equal(4, nodes.Count());
      INode node;
      node = Assert.Single(nodes, x => x.Name == "SharedFile.jpg");
      Assert.Equal(NodeType.File, node.Type);
      Assert.Equal(AuthenticatedTestContext.Inputs.SharedFile.Id, node.Id);
      Assert.Equal(AuthenticatedTestContext.Inputs.SharedFolder.Id, node.ParentId);
      Assert.Equal(AuthenticatedTestContext.Inputs.SharedFile.Size, node.Size);
      Assert.Equal(AuthenticatedTestContext.Inputs.SharedFile.ModificationDate, node.ModificationDate);
      Assert.Equal(AuthenticatedTestContext.Inputs.SharedFile.CreationDate, node.CreationDate);
      Assert.Equal(AuthenticatedTestContext.Inputs.SharedFile.Fingerprint, node.Fingerprint);
      Assert.Equal(2, node.FileAttributes.Length);

      node = Assert.Single(nodes, x => x.Name == "SharedFolder");
      Assert.Equal(NodeType.Root, node.Type);
      Assert.Equal(AuthenticatedTestContext.Inputs.SharedFolder.Id, node.Id);
      Assert.Null(node.ParentId);
      Assert.Equal(0, node.Size);
      Assert.Equal(AuthenticatedTestContext.Inputs.SharedFolder.CreationDate, node.CreationDate);
      Assert.Null(node.ModificationDate);
      Assert.Null(node.Fingerprint);
      Assert.Empty(node.FileAttributes);

      node = Assert.Single(nodes, x => x.Name == "SharedSubFolder");
      Assert.Equal(NodeType.Directory, node.Type);
      Assert.Equal(AuthenticatedTestContext.Inputs.SharedSubFolder.Id, node.Id);
      Assert.Equal(AuthenticatedTestContext.Inputs.SharedFolder.Id, node.ParentId);
      Assert.Equal(0, node.Size);
      Assert.Equal(AuthenticatedTestContext.Inputs.SharedSubFolder.CreationDate, node.CreationDate);
      Assert.Null(node.ModificationDate);
      Assert.Null(node.Fingerprint);
      Assert.Empty(node.FileAttributes);

      node = Assert.Single(nodes, x => x.Name == "SharedFileUpSideDown.jpg");
      Assert.Equal(NodeType.File, node.Type);
      Assert.Equal(AuthenticatedTestContext.Inputs.SharedFileUpSideDown.Id, node.Id);
      Assert.Equal(AuthenticatedTestContext.Inputs.SharedSubFolder.Id, node.ParentId);
      Assert.Equal(AuthenticatedTestContext.Inputs.SharedFileUpSideDown.Size, node.Size);
      Assert.Equal(AuthenticatedTestContext.Inputs.SharedFileUpSideDown.ModificationDate, node.ModificationDate);
      Assert.Equal(AuthenticatedTestContext.Inputs.SharedFileUpSideDown.CreationDate, node.CreationDate);
      Assert.Equal(AuthenticatedTestContext.Inputs.SharedFileUpSideDown.Fingerprint, node.Fingerprint);
      Assert.Equal(2, node.FileAttributes.Length);
    }

    [Fact]
    public void DeserializeNodes_SkipInvalidKeys_Succeeds()
    {
      var data = @"
      {
        'f': [
          {
            'h': 'kBEQkB6B',
            'p': 'kUMDWapK',
            'u': 'tQyS8DbSPXY',
            't': 0,
            'a': 'v03r4dTkEn24_4UMAshsntkEg2dm4HJxupEt-FkasIZfqwG9QbSS-TPpg-ftS0ps0wxa7UEKcCKxu0Dw55kcGR4bbLEcPJ9i8w3WBqxRWdnSyoDt7_pLBZMsSiY1oUjg',
            'k': 'tQyS8DbSPXY:bIUK8KCjsB3-cIAyjWTwxl-vqj_wbxyHvpxdnKPLDCk',
            's': 54161282,
            'ts': 1624367975
          },
          {
            'h': 'AR0VEIwJ',
            'p': '4NUFWaiQ',
            'u': 'tQyS8DbSPXY',
            't': 0,
            'a': 'xkoxdtzKjvB71ooZ7VrKyujxKxs_tDmIlqybvd-Rr-JM38OQ_1xcbD2F-qYgunW7x_1dgB8aFm-8xmfZ0GmefSvSfp-tC3XF2eDlOG6nZoA',
            'k': 'tQyS8DbSPXY:CAAZ9In47mdjTNVgtPoheuvFkFe6sP-bWdeCRVJClvOB5iXmFFIzV-GR7jASHrAnPOTAXXoSHQepGDqr_44Nvi5plYevduF7gvB8FHSUfhpPF2NvqAdFrv8R2SdyOLvX4f41J9CT7gSHS6sYL1l_JweYR7OL7R7GVle30THuW93D3G1FhIKNTyoXlJ9uBGcRZ_gowLT5jjjkgXfc7uRjos2mgumlVZHNY761mBKZaASPnJ12iTXaLLrgZEHcIL9csgOf_BA_C_b7VS6oqTYJha789Z0OrfXLKy9U0ctHQhP6gYp8BeKeqbJ7Of8gaIM6-zC2NfaezZCj8EaWFEp3PG-H',
            's': 23840386,
            'ts': 1624534319
          }
        ]
      }";

      var nodes = JsonConvert.DeserializeObject<GetNodesResponse>(data, new GetNodesResponseConverter(new byte[16])).Nodes;

      Assert.Equal(2, nodes.Length);
      Assert.NotNull(nodes[0].Name);
      Assert.NotNull(nodes[0].Key);
      Assert.NotNull(nodes[0].FullKey);
      Assert.Null(nodes[1].Name);
      Assert.Null(nodes[1].FullKey);
    }
  }
}
