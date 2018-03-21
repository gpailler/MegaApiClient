namespace CG.Web.MegaApiClient
{
  using System;
  using System.Collections.Generic;
  using System.IO;
#if !NET35
  using System.Threading;
#endif

  public partial interface IMegaApiClient
  {
    event EventHandler<ApiRequestFailedEventArgs> ApiRequestFailed;

    bool IsLoggedIn { get; }

    MegaApiClient.LogonSessionToken Login(string email, string password);

    MegaApiClient.LogonSessionToken Login(MegaApiClient.AuthInfos authInfos);

    void Login(MegaApiClient.LogonSessionToken logonSessionToken);

    void Login();

    void LoginAnonymous();

    void Logout();

    IAccountInformation GetAccountInformation();

    IEnumerable<INode> GetNodes();

    IEnumerable<INode> GetNodes(INode parent);

    void Delete(INode node, bool moveToTrash = true);

    INode CreateFolder(string name, INode parent);

    Uri GetDownloadLink(INode node);

#if NET35
    void DownloadFile(INode node, string outputFile);
#else
    void DownloadFile(INode node, string outputFile, CancellationToken? cancellationToken = null);
#endif

#if NET35
    void DownloadFile(Uri uri, string outputFile);
#else
    void DownloadFile(Uri uri, string outputFile, CancellationToken? cancellationToken = null);
#endif

#if NET35
    Stream Download(INode node);
#else
    Stream Download(INode node, CancellationToken? cancellationToken = null);
#endif

#if NET35
    Stream Download(Uri uri);
#else
    Stream Download(Uri uri, CancellationToken? cancellationToken = null);
#endif

    INodeInfo GetNodeFromLink(Uri uri);

    IEnumerable<INode> GetNodesFromLink(Uri uri);

#if NET35
    INode UploadFile(string filename, INode parent);
#else
    INode UploadFile(string filename, INode parent, CancellationToken? cancellationToken = null);
#endif

#if NET35
    INode Upload(Stream stream, string name, INode parent, DateTime? lastModifiedDate = null);
#else
    INode Upload(Stream stream, string name, INode parent, DateTime? modificationDate = null, CancellationToken? cancellationToken = null);
#endif

    INode Move(INode node, INode destinationParentNode);

    INode Rename(INode node, string newName);
  }
}