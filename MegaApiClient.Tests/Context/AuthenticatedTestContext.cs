using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace CG.Web.MegaApiClient.Tests.Context
{
  [CollectionDefinition("AuthenticatedLoginTests")]
  public class AuthenticatedLoginTestsCollection : ICollectionFixture<AuthenticatedTestContext> { }

  public class AuthenticatedTestContext : TestContext, IDisposable
  {
    private const string MegaApiClientPasswordEnvironment = "MEGAAPICLIENT_PASSWORD";
    internal const string Username = "megaapiclient@yopmail.com";
    internal static readonly string Password = Environment.GetEnvironmentVariable(MegaApiClientPasswordEnvironment);

    internal const string FileLink = "https://mega.nz/#!bkwkHC7D!AWJuto8_fhleAI2WG0RvACtKkL_s9tAtvBXXDUp2bQk";
    internal const string FolderLink = "https://mega.nz/#F!e1ogxQ7T!ee4Q_ocD1bSLmNeg9B6kBw";
    internal const string FileId = "P8BBzaTS";
    internal const string FolderId = "e5IjHLLJ";
    internal const string SubFolderId = "CghQlTCa";
    internal const string SubFolderFileId = "X8JhEKJT";

    /*
    Storage layout

    +-Root                                      (bsxVBKLL)
    |   +-SharedFolder                          (e5IjHLLJ) (Outgoing Share)
    |       |-SharedFile.jpg                    (P8BBzaTS)
    |       +-SharedSubFolder                   (CghQlTCa) (Outgoing Share)
    |           |-breadboard.jpg                (X8JhEKJT)
    +-Trash                                     (j0wEGbTZ)
    +-Inbox                                     (zhITTbIJ)
    +-Contacts
      +-SharedRemoteFolder                    (b0I0QDhA) (Incoming Share)
        |-SharedRemoteFile.jpg                (e5wjkSJB)
        +-SharedRemoteSubFolder               (KhZSWI7C) (Incoming Share / Subfolder of SharedRemoteFolder)
          |-SharedRemoteSubFile.jpg           (HtonzYYY)
          +-SharedRemoteSubSubFolder          (z1YCibCT)

    */

    private readonly string[] systemNodes =
    {
      "bsxVBKLL", // Root
      "j0wEGbTZ", // Trash
      "zhITTbIJ", // Inbox
    };

    private readonly string[] permanentFoldersRootNodes =
    {
      FolderId // SharedFolder
    };

    private readonly string[] permanentFoldersNodes =
    {
      SubFolderId, // SharedSubFolder
    };

    private readonly string[] permanentRemoteFoldersNodes =
    {
      "b0I0QDhA", // SharedRemoteFolder
      "KhZSWI7C", // SharedRemoteSubFolder
      "z1YCibCT", // SharedRemoteSubSubFolder
    };

    private readonly string[] permanentFilesNodes =
    {
      FileId, // SharedFile.jpg
      SubFolderFileId, // breadboard.jpg
    };

    private readonly string[] permanentRemoteFilesNodes =
    {
      "e5wjkSJB", // SharedRemoteFile.jpg
      "HtonzYYY", // SharedRemoteSubFile.jpg
    };

    public string PermanentFilesNode
    {
      get { return this.permanentFilesNodes[0]; }
    }

    public virtual void Dispose()
    {
      this.Client.Logout();
    }

    protected override void ConnectClient(IMegaApiClient client)
    {
      Assert.False(string.IsNullOrEmpty(Password), $"Environment variable {MegaApiClientPasswordEnvironment} not set.");
      client.Login(Username, Password);
    }

    protected override IEnumerable<string> GetProtectedNodes()
    {
      return this.systemNodes
        .Concat(this.permanentFoldersRootNodes)
        .Concat(this.permanentFoldersNodes)
        .Concat(this.permanentFilesNodes)
        .Concat(this.permanentRemoteFoldersNodes)
        .Concat(this.permanentRemoteFilesNodes);
    }

    protected override IEnumerable<string> GetPermanentNodes()
    {
      return this.permanentFoldersRootNodes;
    }
  }
}
