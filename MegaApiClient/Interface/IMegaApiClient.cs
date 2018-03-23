namespace CG.Web.MegaApiClient
{
  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.Threading;

  public partial interface IMegaApiClient
  {
    event EventHandler<ApiRequestFailedEventArgs> ApiRequestFailed;

    bool IsLoggedIn { get; }

    MegaApiClient.LogonSessionToken Login(string email, string password);

    MegaApiClient.LogonSessionToken Login(MegaApiClient.AuthInfos authInfos);

    void Login(MegaApiClient.LogonSessionToken logonSessionToken);

    void LoginAnonymous();

    void Logout();

    IAccountInformation GetAccountInformation();

    IEnumerable<INode> GetNodes();

    IEnumerable<INode> GetNodes(INode parent);

    void Delete(INode node, bool moveToTrash = true);

    INode CreateFolder(string name, INode parent);

    Uri GetDownloadLink(INode node);

    void DownloadFile(INode node, string outputFile, CancellationToken? cancellationToken = null);

    void DownloadFile(Uri uri, string outputFile, CancellationToken? cancellationToken = null);

    Stream Download(INode node, CancellationToken? cancellationToken = null);

    Stream Download(Uri uri, CancellationToken? cancellationToken = null);

    INodeInfo GetNodeFromLink(Uri uri);

    IEnumerable<INode> GetNodesFromLink(Uri uri);

    INode UploadFile(string filename, INode parent, CancellationToken? cancellationToken = null);

    INode Upload(Stream stream, string name, INode parent, DateTime? modificationDate = null, CancellationToken? cancellationToken = null);

    INode Move(INode node, INode destinationParentNode);

    INode Rename(INode node, string newName);

    INode UpdateFile(string filename, INode parent, INode nodeToReplace, UpdateMode updateMode, CancellationToken? cancellationToken = null);

    INode Update(Stream stream, INode parent, INode nodeToReplace, UpdateMode updateMode, DateTime? modificationDate = null, CancellationToken? cancellationToken = null);
  }

  public enum UpdateMode
  {
    /// <summary>
    /// Upload new content with same name than existing node (legacy behavior)
    /// </summary>
    Duplicate,

    /// <summary>
    /// Upload new content and move previous node to Trash
    /// </summary>
    Replace,

    /// <summary>
    /// Upload new content and keep old version in file history
    /// </summary>
    Version,
  }
}