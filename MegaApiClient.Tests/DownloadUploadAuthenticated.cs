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
  [Collection(nameof(AuthenticatedTestContext))]
  public class DownloadUploadAuthenticated : DownloadUpload
  {
    public DownloadUploadAuthenticated(AuthenticatedTestContext context, ITestOutputHelper testOutputHelper)
      : base(context, testOutputHelper)
    {
    }

    [Fact]
    public void DownloadNode_ToStream_Succeeds()
    {
      var node = this.GetNode(AuthenticatedTestContext.Inputs.SharedFile.Id);

      using Stream stream = Context.Client.Download(node);
      using Stream expectedStream = new FileStream(GetAbsoluteFilePath("Data/SampleFile.jpg"), FileMode.Open, FileAccess.Read);
      AreStreamsEquivalent(stream, expectedStream);
    }

    [Theory, MemberData(nameof(GetDownloadLinkInvalidParameter))]
    public void GetDownloadLink_InvalidNode_Throws(NodeType? nodeType, Type expectedExceptionType, string expectedMessage)
    {
      var node = nodeType == null ? null : Mock.Of<INode>(x => x.Type == nodeType.Value);
      var exception = Assert.Throws(expectedExceptionType, () => Context.Client.GetDownloadLink(node));
      if (expectedExceptionType == typeof(ArgumentException))
      {
        Assert.Equal(expectedMessage, exception.Message);
      }
    }

    public static IEnumerable<object[]> GetDownloadLinkInvalidParameter
    {
      get
      {
        yield return new object[] { null, typeof(ArgumentNullException), null };
        yield return new object[] { NodeType.Inbox, typeof(ArgumentException), "Invalid node" };
        yield return new object[] { NodeType.Root, typeof(ArgumentException), "Invalid node" };
        yield return new object[] { NodeType.Trash, typeof(ArgumentException), "Invalid node" };
        yield return new object[] { NodeType.File, typeof(ArgumentException), "node must implement INodeCrypto" };
      }
    }

    [Fact]
    public void UploadStream_DownloadLink_Succeeds()
    {
      var data = new byte[123];
      Random.NextBytes(data);

      var parent = GetNode(NodeType.Root);

      using Stream stream = new MemoryStream(data);
      var node = Context.Client.Upload(stream, "test", parent);

      var uri = Context.Client.GetDownloadLink(node);

      stream.Position = 0;
      AreStreamsEquivalent(Context.Client.Download(uri), stream);
    }

    [Theory]
    [JsonInputsData("SharedFile.Id", "FileLink")]
    [JsonInputsData("SharedFolder.Id", "FolderLink")]
    public void GetDownloadLink_ExistingLinks_Succeeds(string id, string expectedLink)
    {
      var node = GetNode(id);

      var link = Context.Client.GetDownloadLink(node);
      Assert.Equal(expectedLink, link.AbsoluteUri);
    }

    [Fact]
    public void GetDownloadLink_FolderNewLink_Succeeds()
    {
      // Create folders structure with subdirectories and file to ensure
      // SharedKey is distributed on all children
      var rootNode = GetNode(NodeType.Root);
      var folderNode = CreateFolderNode(rootNode, "Test");
      var subFolderNode = CreateFolderNode(folderNode, "AA");
      var subFolderNode2 = CreateFolderNode(folderNode, "BB");
      var subSubFolderNode = CreateFolderNode(subFolderNode, "subAA");
      var subSubFileNode = Context.Client.UploadFile(GetAbsoluteFilePath("Data/SampleFile.jpg"), subSubFolderNode);

      Context.Client.GetDownloadLink(folderNode);

      var nodes = Context.Client.GetNodes().ToArray();
      foreach (var node in new[] { folderNode, subFolderNode, subFolderNode2, subSubFolderNode, subSubFileNode })
      {
        var updatedNode = nodes.First(x => x.Id == node.Id);
        Assert.NotNull(((INodeCrypto)updatedNode).SharedKey);
      }
    }

    [Theory(Skip = "SSL Handshake failed on CI")]
    [InlineData(FileAttributeType.Thumbnail, "Data/SampleFile_thumbnail.jpg")]
    [InlineData(FileAttributeType.Preview, "Data/SampleFile_preview.jpg")]
    public void DownloadFileAttribute_ToStream_Succeeds(FileAttributeType fileAttributeType, string expectedFileContent)
    {
      var node = this.GetNode(AuthenticatedTestContext.Inputs.SharedFile.Id);

      using Stream stream = Context.Client.DownloadFileAttribute(node, fileAttributeType);
      using Stream expectedStream = new FileStream(GetAbsoluteFilePath(expectedFileContent), FileMode.Open, FileAccess.Read);
      AreStreamsEquivalent(stream, expectedStream);
    }
  }
}
