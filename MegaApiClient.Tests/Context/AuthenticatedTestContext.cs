using System;
using System.Linq;
using Xunit;

namespace CG.Web.MegaApiClient.Tests.Context
{
  [CollectionDefinition("AuthenticatedLoginTests")]
  public class AuthenticatedLoginTestsCollection : ICollectionFixture<AuthenticatedTestContext> { }

  public class AuthenticatedTestContext : TestContext, IDisposable
  {
    internal const string Username = "megaapiclient@yopmail.com";
    internal const string Password = "megaapiclient";

    /*
    Storage layout

    +-Root                                      (bsxVBKLL)
    |   +-SharedFolder                          (KoRAhTbQ) (Outgoing Share)
    |       |-SharedFile.jpg                    (eooj3IwY)
    |       +-SharedSubFolder                   (nxxWXJAb) (Outgoing Share)
    +-Trash                                     (j0wEGbTZ)
    +-Inbox                                     (zhITTbIJ)
    +-Contacts
        +-SharedRemoteFolder                    (b0I0QDhA) (Incoming Share)
            |-SharedRemoteFile.jpg              (e5wjkSJB)
            +-SharedRemoteSubFolder             (KhZSWI7C) (Incoming Share / Subfolder of SharedRemoteFolder)
                |-SharedRemoteSubFile.jpg       (HtonzYYY)
                +-SharedRemoteSubSubFolder      (z1YCibCT)

    */

    private readonly string[] systemNodes =
    {
      "bsxVBKLL", // Root
      "j0wEGbTZ", // Trash
      "zhITTbIJ", // Inbox
    };

    private readonly string[] permanentFoldersRootNodes =
    {
      "KoRAhTbQ" // SharedFolder
    };

    private readonly string[] permanentFoldersNodes =
    {
      "nxxWXJAb", // SharedSubFolder
    };

    private readonly string[] permanentRemoteFoldersNodes =
    {
      "b0I0QDhA", // SharedRemoteFolder
      "KhZSWI7C", // SharedRemoteSubFolder
      "z1YCibCT", // SharedRemoteSubSubFolder
    };

    private readonly string[] permanentFilesNodes =
    {
      "eooj3IwY", // SharedFile.jpg
    };

    private readonly string[] permanentRemoteFilesNodes =
    {
      "e5wjkSJB", // SharedRemoteFile.jpg
      "HtonzYYY", // SharedRemoteSubFile.jpg
    };

    public AuthenticatedTestContext()
    {
      this.ProtectedNodes = this.systemNodes
        .Concat(this.permanentFoldersRootNodes)
        .Concat(this.permanentFoldersNodes)
        .Concat(this.permanentFilesNodes)
        .Concat(this.permanentRemoteFoldersNodes)
        .Concat(this.permanentRemoteFilesNodes)
        .ToArray();

      this.PermanentRootNodes = this.permanentFoldersRootNodes;
    }

    public string PermanentFilesNode
    {
      get { return this.permanentFilesNodes[0]; }
    }

    protected override void ConnectClient(IMegaApiClient client)
    {
      client.Login(Username, Password);
    }

    public virtual void Dispose()
    {
      this.Client.Logout();
    }
  }
}
