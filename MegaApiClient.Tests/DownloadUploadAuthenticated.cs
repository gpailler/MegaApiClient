using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CG.Web.MegaApiClient.Tests.Context;
using Moq;
using Xunit;

namespace CG.Web.MegaApiClient.Tests
{
  [Collection("AuthenticatedLoginTests")]
  public class DownloadUploadAuthenticated : DownloadUpload
  {
    public DownloadUploadAuthenticated(AuthenticatedTestContext context)
      : base(context)
    {
    }

    [Fact]
    public void DownloadNode_ToStream_Succeeds()
    {
      var node = this.GetNode(((AuthenticatedTestContext)this.context).PermanentFilesNode);

      using (Stream stream = this.context.Client.Download(node))
      {
        using (Stream expectedStream = new FileStream(this.GetAbsoluteFilePath("Data/SampleFile.jpg"), FileMode.Open))
        {
          this.AreStreamsEquivalent(stream, expectedStream);
        }
      }
    }

    [Theory, MemberData(nameof(GetDownloadLinkInvalidParameter))]
    public void GetDownloadLink_InvalidNode_Throws(INode node, Type expectedExceptionType, string expectedMessage)
    {
      var exception = Assert.Throws(expectedExceptionType, () => this.context.Client.GetDownloadLink(node));
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

        foreach (NodeType nodeType in new[] {NodeType.Inbox, NodeType.Root, NodeType.Trash})
        {
          yield return new object[] { Mock.Of<INode>(x => x.Type == nodeType), typeof(ArgumentException), "Invalid node" };
        }

        yield return new object[] { Mock.Of<INode>(x => x.Type == NodeType.File), typeof(ArgumentException), "node must implement INodeCrypto" };
      }
    }

    [Fact]
    public void UploadStream_DownloadLink_Succeeds()
    {
      byte[] data = new byte[123];
      this.random.NextBytes(data);

      var parent = this.GetNode(NodeType.Root);

      using (Stream stream = new MemoryStream(data))
      {
        var node = this.context.Client.Upload(stream, "test", parent);

        var uri = this.context.Client.GetDownloadLink(node);

        stream.Position = 0;
        this.AreStreamsEquivalent(this.context.Client.Download(uri), stream);
      }
    }

    [Theory]
    [InlineData("eooj3IwY", "https://mega.nz/#!ulISSQIb!RSz1DoCSGANrpphQtkr__uACIUZsFkiPWEkldOHNO20")]
    [InlineData("KoRAhTbQ", "https://mega.nz/#F!6kgE3YIQ!W_8GYHXH-COtmfWxOkMCFQ")]
    public void GetDownloadLink_ExistingLinks_Succeeds(string id, string expectedLink)
    {
      var node = this.GetNode(id);

      var link = this.context.Client.GetDownloadLink(node);
      Assert.Equal(expectedLink, link.AbsoluteUri);
    }

    [Fact]
    public void GetDownloadLink_FolderNewLink_Succeeds()
    {
      // Create folders structure with subdirectories and file to ensure
      // SharedKey is distributed on all children
      var rootNode = this.GetNode(NodeType.Root);
      var folderNode = this.CreateFolderNode(rootNode, "Test");
      var subFolderNode = this.CreateFolderNode(folderNode, "AA");
      var subFolderNode2 = this.CreateFolderNode(folderNode, "BB");
      var subSubFolderNode = this.CreateFolderNode(subFolderNode, "subAA");
      var subSubFileNode = this.context.Client.UploadFile(this.GetAbsoluteFilePath("Data/SampleFile.jpg"), subSubFolderNode);

      this.context.Client.GetDownloadLink(folderNode);

      var nodes = this.context.Client.GetNodes().ToArray();
      foreach (var node in new[] {folderNode, subFolderNode, subFolderNode2, subSubFolderNode, subSubFileNode})
      {
        var updatedNode = nodes.First(x => x.Id == node.Id);
        Assert.NotNull(((INodeCrypto) updatedNode).SharedKey);
      }
    }
  }
}
