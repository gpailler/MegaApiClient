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

    void Login(string email, string password);

    void Login(MegaApiClient.AuthInfos authInfos);

    void LoginAnonymous();

    void Logout();

    IAccountInformation GetAccountInformation();

    IEnumerable<INode> GetNodes();

    IEnumerable<INode> GetNodes(INode parent);

    void Delete(INode node, bool moveToTrash = true);

    INode CreateFolder(string name, INode parent);

    Uri GetDownloadLink(INode node);

    void DownloadFile(INode node, string outputFile);

    void DownloadFile(Uri uri, string outputFile);

    Stream Download(INode node);

    Stream Download(Uri uri);

    INodePublic GetNodeFromLink(Uri uri);

    INode UploadFile(string filename, INode parent);

#if NET35
     INode Upload(Stream stream, string name, INode parent, DateTime? lastModifiedDate = null);
#else
    INode Upload(Stream stream, string name, INode parent, DateTime? modificationDate = null, CancellationToken? cancellationToken = null);
 #endif

    INode Move(INode node, INode destinationParentNode);

    INode Rename(INode node, string newName);
  }
}