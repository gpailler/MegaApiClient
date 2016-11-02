namespace CG.Web.MegaApiClient
{
  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.Threading.Tasks;

  public partial interface IMegaApiClient
  {
    long ReportProgressChunkSize { get; set; }

    Task LoginAsync(string email, string password);

    Task LoginAsync(MegaApiClient.AuthInfos authInfos);

    Task LoginAnonymousAsync();

    Task LogoutAsync();

    Task<IAccountInformation> GetAccountInformationAsync();

    Task<IEnumerable<INode>> GetNodesAsync();

    Task<IEnumerable<INode>> GetNodesAsync(INode parent);

    Task<INode> CreateFolderAsync(string name, INode parent);

    Task DeleteAsync(INode node, bool moveToTrash = true);

    Task<INode> MoveAsync(INode sourceNode, INode destinationParentNode);

    Task<INode> RenameAsync(INode sourceNode, string newName);

    Task<Uri> GetDownloadLinkAsync(INode node);

    Task<Stream> DownloadAsync(INode node, IProgress<double> progress);

    Task<Stream> DownloadAsync(Uri uri, IProgress<double> progress);

    Task DownloadFileAsync(INode node, string outputFile, IProgress<double> progress);

    Task DownloadFileAsync(Uri uri, string outputFile, IProgress<double> progress);

    Task<INode> UploadAsync(Stream stream, string name, INode parent, IProgress<double> progress);

    Task<INode> UploadFileAsync(string filename, INode parent, IProgress<double> progress);

    Task<INodePublic> GetNodeFromLinkAsync(Uri uri);
  }
}