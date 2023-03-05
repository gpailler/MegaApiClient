using System;
using System.Linq;
using CG.Web.MegaApiClient.Tests.Context;
using Xunit;
using Xunit.Abstractions;

namespace CG.Web.MegaApiClient.Tests
{
  [Collection(nameof(AuthenticatedTestContext))]
  public class NodeOperationsAuthenticated : NodeOperations
  {
    public NodeOperationsAuthenticated(AuthenticatedTestContext context, ITestOutputHelper testOutputHelper)
        : base(context, testOutputHelper)
    {
    }

    [Theory]
    [JsonInputsData(new object[] { NodeType.Root, null }, new string[] { "Root.Id", "", null, "Root.CreationDate", null, null })]
    [JsonInputsData(new object[] { NodeType.Trash, null }, new string[] { "Trash.Id", "", null, "Trash.CreationDate", null, null })]
    [JsonInputsData(new object[] { NodeType.Inbox, null }, new string[] { "Inbox.Id", "", null, "Inbox.CreationDate", null, null })]
    [JsonInputsData(new object[] { NodeType.Directory, "SharedFolder" }, new string[] { "SharedFolder.Id", "Root.Id", null, "SharedFolder.CreationDate", null, null })]
    [JsonInputsData(new object[] { NodeType.File, "SharedFile.jpg" }, new string[] { "SharedFile.Id", "SharedFolder.Id", "SharedFile.Size", "SharedFile.CreationDate", "SharedFile.ModificationDate", "SharedFile.Fingerprint" })]
    [JsonInputsData(new object[] { NodeType.Directory, "SharedSubFolder" }, new string[] { "SharedSubFolder.Id", "SharedFolder.Id", null, "SharedSubFolder.CreationDate", null, null })]
    [JsonInputsData(new object[] { NodeType.File, "SharedFileUpSideDown.jpg" }, new string[] { "SharedFileUpSideDown.Id", "SharedSubFolder.Id", "SharedFileUpSideDown.Size", "SharedFileUpSideDown.CreationDate", "SharedFileUpSideDown.ModificationDate", "SharedFileUpSideDown.Fingerprint" })]
    [JsonInputsData(new object[] { NodeType.Directory, "SharedRemoteFolder" }, new string[] { "SharedRemoteFolder.Id", "SharedRemoteFolder.ParentId", null, "SharedRemoteFolder.CreationDate", null, null })]
    [JsonInputsData(new object[] { NodeType.File, "SharedRemoteFile.jpg" }, new string[] { "SharedRemoteFile.Id", "SharedRemoteFolder.Id", "SharedRemoteFile.Size", "SharedRemoteFile.CreationDate", "SharedRemoteFile.ModificationDate", "SharedRemoteFile.Fingerprint" })]
    [JsonInputsData(new object[] { NodeType.Directory, "SharedRemoteSubFolder" }, new string[] { "SharedRemoteSubFolder.Id", "SharedRemoteFolder.Id", null, "SharedRemoteSubFolder.CreationDate", null, null })]
    [JsonInputsData(new object[] { NodeType.File, "SharedRemoteSubFile.jpg" }, new string[] { "SharedRemoteSubFile.Id", "SharedRemoteSubFolder.Id", "SharedRemoteSubFile.Size", "SharedRemoteSubFile.CreationDate", "SharedRemoteSubFile.ModificationDate", "SharedRemoteSubFile.Fingerprint" })]
    [JsonInputsData(new object[] { NodeType.Directory, "SharedRemoteSubSubFolder" }, new string[] { "SharedRemoteSubSubFolder.Id", "SharedRemoteSubFolder.Id", null, "SharedRemoteSubSubFolder.CreationDate", null, null })]
    public void Validate_PermanentNodes_Succeeds(
        NodeType expectedNodeType,
        string expectedName,
        string id,
        string expectedParent,
        long expectedSize,
        DateTime expectedCreationDate,
        DateTime? expectedModificationDate,
        string expectedFingerprint
        )
    {
      var node = GetNode(id);

      Assert.Equal(expectedNodeType, node.Type);
      Assert.Equal(expectedParent, node.ParentId);
      Assert.Equal(expectedName, node.Name);
      Assert.Equal(expectedSize, node.Size);
      Assert.Equal(expectedCreationDate, node.CreationDate);
      Assert.Equal(expectedModificationDate, node.ModificationDate);
      Assert.Equal(expectedFingerprint, node.Fingerprint);
    }

    [Theory]
    [JsonInputsData(new object[] { NodeType.Root }, new string[] { "SharedFile.Size", "SharedFileUpSideDown.Size", "SampleZipFile.Size" })]
    [InlineData(NodeType.Inbox, 0, 0, 0)]
    [InlineData(NodeType.Trash, 0, 0, 0)]
    public void GetFoldersize_FromNodeType_Succeeds(NodeType nodeType, long sharedFileSize, long sharedFileUpSideDownSize, long sampleZipFile)
    {
      var node = GetNode(nodeType);
      var expectedSize = sharedFileSize + sharedFileUpSideDownSize + sampleZipFile;
      Assert.Equal(expectedSize, node.GetFolderSize(Context.Client));
      Assert.Equal(expectedSize, node.GetFolderSizeAsync(Context.Client).Result);
      Assert.Equal(expectedSize, node.GetFolderSize(Context.Client.GetNodes()));
      Assert.Equal(expectedSize, node.GetFolderSizeAsync(Context.Client.GetNodes()).Result);
    }

    [Fact]
    public void GetFoldersize_FromFile_Throws()
    {
      var node = Context.Client.GetNodes().First(x => x.Type == NodeType.File);
      Assert.Throws<InvalidOperationException>(() => node.GetFolderSize(Context.Client));
      var aggregateException = Assert.Throws<AggregateException>(() => node.GetFolderSizeAsync(Context.Client).Result);
      Assert.IsType<InvalidOperationException>(aggregateException.GetBaseException());
      Assert.Throws<InvalidOperationException>(() => node.GetFolderSize(Context.Client.GetNodes()));
      aggregateException = Assert.Throws<AggregateException>(() => node.GetFolderSizeAsync(Context.Client.GetNodes()).Result);
      Assert.IsType<InvalidOperationException>(aggregateException.GetBaseException());
    }

    [Theory]
    [JsonInputsData("SharedFolder.Id", "SharedFile.Size", "SharedFileUpSideDown.Size")]
    [JsonInputsData("SharedSubFolder.Id", null, "SharedFileUpSideDown.Size")]
    public void GetFoldersize_FromDirectory_Succeeds(string nodeId, long size1, long size2)
    {
      var node = GetNode(nodeId);
      var expectedSize = size1 + size2;
      Assert.Equal(expectedSize, node.GetFolderSize(Context.Client));
      Assert.Equal(expectedSize, node.GetFolderSizeAsync(Context.Client).Result);
      Assert.Equal(expectedSize, node.GetFolderSize(Context.Client.GetNodes()));
      Assert.Equal(expectedSize, node.GetFolderSizeAsync(Context.Client.GetNodes()).Result);
    }

    [Fact]
    public void GetFileAttributes_FromNode_Succeeds()
    {
      var node = this.GetNode(AuthenticatedTestContext.Inputs.SharedFile.Id);

      Assert.Equal(2, node.FileAttributes.Length);

      var fileAttribute = node.FileAttributes[0];
      Assert.Equal(AuthenticatedTestContext.Inputs.SharedFile.Metadata[0].Id, fileAttribute.Handle);
      Assert.Equal(AuthenticatedTestContext.Inputs.SharedFile.Metadata[0].AttributeId, fileAttribute.Id);
      Assert.Equal(FileAttributeType.Thumbnail, fileAttribute.Type);

      fileAttribute = node.FileAttributes[1];
      Assert.Equal(AuthenticatedTestContext.Inputs.SharedFile.Metadata[1].Id, fileAttribute.Handle);
      Assert.Equal(AuthenticatedTestContext.Inputs.SharedFile.Metadata[1].AttributeId, fileAttribute.Id);
      Assert.Equal(FileAttributeType.Preview, fileAttribute.Type);
    }

    [Fact]
    public void GetAccountInformation_AuthenticatedUser_Succeeds()
    {
      var accountInformation = Context.Client.GetAccountInformation();

      Assert.NotNull(accountInformation);
      Assert.Equal(AuthenticatedTestContext.Inputs.TotalQuota, accountInformation.TotalQuota);
      Assert.Equal(AuthenticatedTestContext.Inputs.SharedFile.Size + AuthenticatedTestContext.Inputs.SharedFileUpSideDown.Size + AuthenticatedTestContext.Inputs.SampleZipFile.Size, accountInformation.UsedQuota);
    }
  }
}
