namespace CG.Web.MegaApiClient
{
  using System;
  using System.Collections.Generic;
  using System.IO;

  public partial interface IMegaApiClient
  {
    int BufferSize { get; set; }

    int ChunksPackSize { get; set; }

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

    INode Upload(Stream stream, string name, INode parent);

    INode Move(INode node, INode destinationParentNode);

    INode Rename(INode node, string newName);
  }
}