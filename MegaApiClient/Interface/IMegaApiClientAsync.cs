#if ASYNC
namespace CG.Web.MegaApiClient
{
  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.Threading;
  using System.Threading.Tasks;

  public partial interface IMegaApiClient
  {
    Task<MegaApiClient.LogonSessionToken> LoginAsync(string email, string password);

    Task<MegaApiClient.LogonSessionToken> LoginAsync(MegaApiClient.AuthInfos authInfos);

    Task LoginAsync(MegaApiClient.LogonSessionToken logonSessionToken);

    Task LoginAsync();

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

    Task<Stream> DownloadAsync(INode node, IProgress<double> progress, CancellationToken? cancellationToken = null);

    Task<Stream> DownloadAsync(Uri uri, IProgress<double> progress, CancellationToken? cancellationToken = null);

    Task DownloadFileAsync(INode node, string outputFile, IProgress<double> progress, CancellationToken? cancellationToken = null);

    Task DownloadFileAsync(Uri uri, string outputFile, IProgress<double> progress, CancellationToken? cancellationToken = null);

    Task<INode> UploadAsync(Stream stream, string name, INode parent, IProgress<double> progress, DateTime? modificationDate = null, CancellationToken? cancellationToken = null);

    Task<INode> UploadFileAsync(string filename, INode parent, IProgress<double> progress, CancellationToken? cancellationToken = null);

    Task<INodeInfo> GetNodeFromLinkAsync(Uri uri);

    Task<IEnumerable<INode>> GetNodesFromLinkAsync(Uri uri);
  }
}
#endif