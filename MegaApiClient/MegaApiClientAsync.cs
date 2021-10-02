#if !NET40
namespace CG.Web.MegaApiClient
{
  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.Threading;
  using System.Threading.Tasks;

  public partial class MegaApiClient : IMegaApiClient
  {
#region Public async methods

    public Task<LogonSessionToken> LoginAsync(string email, string password, string mfaKey = null)
    {
      return Task.Run(() => Login(email, password, mfaKey));
    }

    public Task<LogonSessionToken> LoginAsync(AuthInfos authInfos)
    {
      return Task.Run(() => Login(authInfos));
    }

    public Task LoginAsync(LogonSessionToken logonSessionToken)
    {
      return Task.Run(() => Login(logonSessionToken));
    }

    public Task LoginAsync()
    {
      return Task.Run(() => Login());
    }

    public Task LoginAnonymousAsync()
    {
      return Task.Run(() => LoginAnonymous());
    }

    public Task LogoutAsync()
    {
      return Task.Run(() => Logout());
    }

    public Task<string> GetRecoveryKeyAsync()
    {
      return Task.FromResult(GetRecoveryKey());
    }

    public Task<IAccountInformation> GetAccountInformationAsync()
    {
      return Task.Run(() => GetAccountInformation());
    }

    public Task<IEnumerable<ISession>> GetSessionsHistoryAsync()
    {
      return Task.Run(() => GetSessionsHistory());
    }

    public Task<IEnumerable<INode>> GetNodesAsync()
    {
      return Task.Run(() => GetNodes());
    }

    public Task<IEnumerable<INode>> GetNodesAsync(INode parent)
    {
      return Task.Run(() => GetNodes(parent));
    }

    public Task<INode> CreateFolderAsync(string name, INode parent)
    {
      return Task.Run(() => CreateFolder(name, parent));
    }

    public Task DeleteAsync(INode node, bool moveToTrash = true)
    {
      return Task.Run(() => Delete(node, moveToTrash));
    }

    public Task<INode> MoveAsync(INode sourceNode, INode destinationParentNode)
    {
      return Task.Run(() => Move(sourceNode, destinationParentNode));
    }

    public Task<INode> RenameAsync(INode sourceNode, string newName)
    {
      return Task.Run(() => Rename(sourceNode, newName));
    }

    public Task<Uri> GetDownloadLinkAsync(INode node)
    {
      return Task.Run(() => GetDownloadLink(node));
    }

    public Task<Stream> DownloadAsync(INode node, IProgress<double> progress = null, CancellationToken? cancellationToken = null)
    {
      return Task.Run(() =>
      {
        return (Stream)new ProgressionStream(Download(node, cancellationToken), progress, _options.ReportProgressChunkSize);
      }, cancellationToken.GetValueOrDefault());
    }

    public Task<Stream> DownloadAsync(Uri uri, IProgress<double> progress = null, CancellationToken? cancellationToken = null)
    {
      return Task.Run(() =>
      {
        return (Stream)new ProgressionStream(Download(uri, cancellationToken), progress, _options.ReportProgressChunkSize);
      }, cancellationToken.GetValueOrDefault());
    }

    public Task DownloadFileAsync(INode node, string outputFile, IProgress<double> progress = null, CancellationToken? cancellationToken = null)
    {
      return Task.Run(() =>
      {
        using (Stream stream = new ProgressionStream(Download(node, cancellationToken), progress, _options.ReportProgressChunkSize))
        {
          SaveStream(stream, outputFile);
        }
      }, cancellationToken.GetValueOrDefault());
    }

    public Task DownloadFileAsync(Uri uri, string outputFile, IProgress<double> progress = null, CancellationToken? cancellationToken = null)
    {
      return Task.Run(() =>
      {
        if (string.IsNullOrEmpty(outputFile))
        {
          throw new ArgumentNullException("outputFile");
        }

        using (Stream stream = new ProgressionStream(Download(uri, cancellationToken), progress, _options.ReportProgressChunkSize))
        {
          SaveStream(stream, outputFile);
        }
      }, cancellationToken.GetValueOrDefault());
    }

    public Task<INode> UploadAsync(Stream stream, string name, INode parent, IProgress<double> progress = null, DateTime? modificationDate = null, CancellationToken? cancellationToken = null)
    {
      return Task.Run(() =>
      {
        if (stream == null)
        {
          throw new ArgumentNullException("stream");
        }

        using (Stream progressionStream = new ProgressionStream(stream, progress, _options.ReportProgressChunkSize))
        {
          return Upload(progressionStream, name, parent, modificationDate, cancellationToken);
        }
      }, cancellationToken.GetValueOrDefault());
    }

    public Task<INode> UploadFileAsync(string filename, INode parent, IProgress<double> progress = null, CancellationToken? cancellationToken = null)
    {
      return Task.Run(() =>
      {
        var modificationDate = File.GetLastWriteTime(filename);
        using (Stream stream = new ProgressionStream(new FileStream(filename, FileMode.Open, FileAccess.Read), progress, _options.ReportProgressChunkSize))
        {
          return Upload(stream, Path.GetFileName(filename), parent, modificationDate, cancellationToken);
        }
      }, cancellationToken.GetValueOrDefault());
    }

    public Task<INode> GetNodeFromLinkAsync(Uri uri)
    {
      return Task.Run(() => GetNodeFromLink(uri));
    }

    public Task<IEnumerable<INode>> GetNodesFromLinkAsync(Uri uri)
    {
      return Task.Run(() => GetNodesFromLink(uri));
    }

    public Task<AuthInfos> GenerateAuthInfosAsync(string email, string password)
    {
      return Task.Run(() => GenerateAuthInfos(email, password));
    }

    public Task<AuthInfos> GenerateAuthInfosAsync(string email, string password, string mfaKey)
    {
      return Task.Run(() => GenerateAuthInfos(email, password, mfaKey));
    }

    public Task<Stream> DownloadFileAttributeAsync(INode node, FileAttributeType fileAttributeType, CancellationToken? cancellationToken = null)
    {
      return Task.Run(() => DownloadFileAttribute(node, fileAttributeType, cancellationToken));
    }
#endregion
  }
}
#endif
