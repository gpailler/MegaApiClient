using System;
using CG.Web.MegaApiClient.Tests.Context;
using Xunit;
using Xunit.Abstractions;

namespace CG.Web.MegaApiClient.Tests
{
  [Collection("AuthenticatedLoginTests")]
  public class NodeOperationsAuthenticated : NodeOperations
  {
    public NodeOperationsAuthenticated(AuthenticatedTestContext context, ITestOutputHelper testOutputHelper)
        : base(context, testOutputHelper)
    {
    }

    [Theory]
    [InlineData(AuthenticatedTestContext.FolderId, "bsxVBKLL", NodeType.Directory, "SharedFolder", 0, "2017-07-11T10:48:00.0000000+07:00", null)]
    [InlineData(AuthenticatedTestContext.SubFolderId, AuthenticatedTestContext.FolderId, NodeType.Directory, "SharedSubFolder", 0, "2017-07-11T10:48:01.0000000+07:00", null)]
    [InlineData(AuthenticatedTestContext.FileId, AuthenticatedTestContext.FolderId, NodeType.File, "SharedFile.jpg", 523265, "2017-07-11T10:48:10.0000000+07:00", "2015-07-14T14:04:51.0000000+08:00")]
    [InlineData("b0I0QDhA", "u4IgDb5K", NodeType.Directory, "SharedRemoteFolder", 0, "2015-05-21T02:35:22.0000000+08:00", null)]
    [InlineData("e5wjkSJB", "b0I0QDhA", NodeType.File, "SharedRemoteFile.jpg", 523265, "2015-05-21T02:36:06.0000000+08:00", "2015-05-19T09:39:50.0000000+08:00")]
    [InlineData("KhZSWI7C", "b0I0QDhA", NodeType.Directory, "SharedRemoteSubFolder", 0, "2015-07-14T17:05:03.0000000+08:00", null)]
    [InlineData("HtonzYYY", "KhZSWI7C", NodeType.File, "SharedRemoteSubFile.jpg", 523265, "2015-07-14T18:06:27.0000000+08:00", "2015-05-27T02:42:21.0000000+08:00")]
    [InlineData("z1YCibCT", "KhZSWI7C", NodeType.Directory, "SharedRemoteSubSubFolder", 0, "2015-07-14T18:01:56.0000000+08:00", null)]
    public void Validate_PermanentNodes_Succeeds(
        string id,
        string expectedParent,
        NodeType expectedNodeType,
        string expectedName,
        long expectedSize,
        string expectedCreationDate,
        string expectedModificationDate
        )
    {
      var node = this.GetNode(id);

      Assert.Equal(expectedNodeType, node.Type);
      Assert.Equal(expectedParent,node.ParentId);
      Assert.Equal(expectedName, node.Name);
      Assert.Equal(expectedSize, node.Size);
      Assert.Equal(DateTime.Parse(expectedCreationDate), node.CreationDate);
      Assert.Equal(expectedModificationDate == null ? (DateTime?)null : DateTime.Parse(expectedModificationDate), node.ModificationDate);
    }
  }
}
