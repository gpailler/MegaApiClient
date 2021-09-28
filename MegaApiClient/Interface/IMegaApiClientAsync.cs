﻿#if !NET40
namespace CG.Web.MegaApiClient
{
  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.Threading;
  using System.Threading.Tasks;

  public partial interface IMegaApiClient
  {
    Task<MegaApiClient.LogonSessionToken> LoginAsync(string email, string password, string mfaKey = null);

    Task<MegaApiClient.LogonSessionToken> LoginAsync(MegaApiClient.AuthInfos authInfos);

    Task LoginAsync(MegaApiClient.LogonSessionToken logonSessionToken);

    Task LoginAsync();

    Task LoginAnonymousAsync();

    Task LogoutAsync();

    Task<string> GetRecoveryKeyAsync();

    Task<IAccountInformation> GetAccountInformationAsync();

    Task<IEnumerable<ISession>> GetSessionsHistoryAsync();

    Task<IEnumerable<INode>> GetNodesAsync();

    Task<IEnumerable<INode>> GetNodesAsync(INode parent);

    Task<INode> CreateFolderAsync(string name, INode parent);

    Task DeleteAsync(INode node, bool moveToTrash = true);

    Task<INode> MoveAsync(INode sourceNode, INode destinationParentNode);

    Task<INode> RenameAsync(INode sourceNode, string newName);

    Task<Uri> GetDownloadLinkAsync(INode node);

    Task<Stream> DownloadAsync(INode node, IProgress<double> progress = null, CancellationToken? cancellationToken = null);

    Task<Stream> DownloadAsync(Uri uri, IProgress<double> progress = null, CancellationToken? cancellationToken = null);

    Task DownloadFileAsync(INode node, string outputFile, IProgress<double> progress = null, CancellationToken? cancellationToken = null);

    Task DownloadFileAsync(Uri uri, string outputFile, IProgress<double> progress = null, CancellationToken? cancellationToken = null);

    Task<INode> UploadAsync(Stream stream, string name, INode parent, IProgress<double> progress = null, DateTime? modificationDate = null, CancellationToken? cancellationToken = null);

    Task<INode> UploadFileAsync(string filename, INode parent, IProgress<double> progress = null, CancellationToken? cancellationToken = null);

    Task<INode> GetNodeFromLinkAsync(Uri uri);

    Task<IEnumerable<INode>> GetNodesFromLinkAsync(Uri uri);

    Task<MegaApiClient.AuthInfos> GenerateAuthInfosAsync(string email, string password);

    Task<MegaApiClient.AuthInfos> GenerateAuthInfosAsync(string email, string password, string mfaKey);

    Task<Stream> DownloadFileAttributeAsync(INode node, FileAttributeType fileAttributeType, CancellationToken? cancellationToken = null);
  }
}
#endif
