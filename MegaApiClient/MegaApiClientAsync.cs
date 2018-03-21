#if ASYNC
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

    public Task<LogonSessionToken> LoginAsync(string email, string password)
    {
      return Task.Run(() => this.Login(email, password));
    }

    public Task<LogonSessionToken> LoginAsync(AuthInfos authInfos)
    {
      return Task.Run(() => this.Login(authInfos));
    }

    public Task LoginAsync(LogonSessionToken logonSessionToken)
    {
      return Task.Run(() => this.Login(logonSessionToken));
    }

    public Task LoginAsync()
    {
      return Task.Run(() => this.Login());
    }

    public Task LoginAnonymousAsync()
    {
      return Task.Run(() => this.LoginAnonymous());
    }

    public Task LogoutAsync()
    {
      return Task.Run(() => this.Logout());
    }

    public Task<IAccountInformation> GetAccountInformationAsync()
    {
      return Task.Run(() => this.GetAccountInformation());
    }

    public Task<IEnumerable<INode>> GetNodesAsync()
    {
      return Task.Run(() => this.GetNodes());
    }

    public Task<IEnumerable<INode>> GetNodesAsync(INode parent)
    {
      return Task.Run(() => this.GetNodes(parent));
    }

    public Task<INode> CreateFolderAsync(string name, INode parent)
    {
      return Task.Run(() => this.CreateFolder(name, parent));
    }

    public Task DeleteAsync(INode node, bool moveToTrash = true)
    {
      return Task.Run(() => this.Delete(node, moveToTrash));
    }

    public Task<INode> MoveAsync(INode sourceNode, INode destinationParentNode)
    {
      return Task.Run(() => this.Move(sourceNode, destinationParentNode));
    }

    public Task<INode> RenameAsync(INode sourceNode, string newName)
    {
      return Task.Run(() => this.Rename(sourceNode, newName));
    }

    public Task<Uri> GetDownloadLinkAsync(INode node)
    {
      return Task.Run(() => this.GetDownloadLink(node));
    }

    public Task<Stream> DownloadAsync(INode node, IProgress<double> progress, CancellationToken? cancellationToken = null)
    {
      return Task.Run(() =>
      {
        return (Stream)new ProgressionStream(this.Download(node, cancellationToken), progress, this.options.ReportProgressChunkSize);
      }, cancellationToken.GetValueOrDefault());
    }

    public Task<Stream> DownloadAsync(Uri uri, IProgress<double> progress, CancellationToken? cancellationToken = null)
    {
      return Task.Run(() =>
      {
        return (Stream)new ProgressionStream(this.Download(uri, cancellationToken), progress, this.options.ReportProgressChunkSize);
      }, cancellationToken.GetValueOrDefault());
    }

    public Task DownloadFileAsync(INode node, string outputFile, IProgress<double> progress, CancellationToken? cancellationToken = null)
    {
      return Task.Run(() =>
      {
        using (Stream stream = new ProgressionStream(this.Download(node, cancellationToken), progress, this.options.ReportProgressChunkSize))
        {
          this.SaveStream(stream, outputFile);
        }
      }, cancellationToken.GetValueOrDefault());
    }

    public Task DownloadFileAsync(Uri uri, string outputFile, IProgress<double> progress, CancellationToken? cancellationToken = null)
    {
      return Task.Run(() =>
      {
        if (string.IsNullOrEmpty(outputFile))
        {
          throw new ArgumentNullException("outputFile");
        }

        using (Stream stream = new ProgressionStream(this.Download(uri, cancellationToken), progress, this.options.ReportProgressChunkSize))
        {
          this.SaveStream(stream, outputFile);
        }
      }, cancellationToken.GetValueOrDefault());
    }

    public Task<INode> UploadAsync(Stream stream, string name, INode parent, IProgress<double> progress, DateTime? modificationDate = null, CancellationToken? cancellationToken = null)
    {
      return Task.Run(() =>
      {
        if (stream == null)
        {
          throw new ArgumentNullException("stream");
        }

        using (Stream progressionStream = new ProgressionStream(stream, progress, this.options.ReportProgressChunkSize))
        {
          return this.Upload(progressionStream, name, parent, modificationDate, cancellationToken);
        }
      }, cancellationToken.GetValueOrDefault());
    }

    public Task<INode> UploadFileAsync(string filename, INode parent, IProgress<double> progress, CancellationToken? cancellationToken = null)
    {
      return Task.Run(() =>
      {
        DateTime modificationDate = File.GetLastWriteTime(filename);
        using (Stream stream = new ProgressionStream(new FileStream(filename, FileMode.Open, FileAccess.Read), progress, this.options.ReportProgressChunkSize))
        {
          return this.Upload(stream, Path.GetFileName(filename), parent, modificationDate, cancellationToken);
        }
      }, cancellationToken.GetValueOrDefault());
    }

    public Task<INodeInfo> GetNodeFromLinkAsync(Uri uri)
    {
      return Task.Run(() => this.GetNodeFromLink(uri));
    }

    public Task<IEnumerable<INode>> GetNodesFromLinkAsync(Uri uri)
    {
      return Task.Run(() => this.GetNodesFromLink(uri));
    }

    #endregion
  }
}
#endif