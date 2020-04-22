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

    MegaApiClient.LogonSessionToken Login(string email, string password, string mfaKey);

    MegaApiClient.LogonSessionToken Login(MegaApiClient.AuthInfos authInfos);

    void Login(MegaApiClient.LogonSessionToken logonSessionToken);

    void Login();

    void LoginAnonymous();

    void Logout();

    string GetRecoveryKey();

    IAccountInformation GetAccountInformation();

    IEnumerable<ISession> GetSessionsHistory();

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

    MegaApiClient.AuthInfos GenerateAuthInfos(string email, string password, string mfaKey = null);
  }
}
