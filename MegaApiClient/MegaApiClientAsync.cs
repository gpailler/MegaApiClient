namespace CG.Web.MegaApiClient
{
  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.Threading.Tasks;

  public partial class MegaApiClient : IMegaApiClient
  {
    public static long ReportProgressChunkSize = 1024 * 50;

    #region Public async methods

    public Task LoginAsync(string email, string password)
    {
      return Task.Run(() => this.Login(email, password));
    }

    public Task LoginAsync(AuthInfos authInfos)
    {
      return Task.Run(() => this.Login(authInfos));
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

    public Task<Uri> GetDownloadLinkAsync(INode node)
    {
      return Task.Run(() => this.GetDownloadLink(node));
    }

    public Task<Stream> DownloadAsync(INode node, IProgress<double> progress)
    {
      return Task.Run(() =>
      {
        return (Stream)new ProgressionStream(this.Download(node), progress);
      });
    }

    public Task<Stream> DownloadAsync(Uri uri, IProgress<double> progress)
    {
      return Task.Run(() =>
      {
        return (Stream)new ProgressionStream(this.Download(uri), progress);
      });
    }

    public Task DownloadFileAsync(INode node, string outputFile, IProgress<double> progress)
    {
      return Task.Run(() =>
      {
        using (Stream stream = new ProgressionStream(this.Download(node), progress))
        {
          this.SaveStream(stream, outputFile);
        }
      });
    }

    public Task DownloadFileAsync(Uri uri, string outputFile, IProgress<double> progress)
    {
      return Task.Run(() =>
      {
        if (string.IsNullOrEmpty(outputFile))
        {
          throw new ArgumentNullException("outputFile");
        }

        using (Stream stream = new ProgressionStream(this.Download(uri), progress))
        {
          this.SaveStream(stream, outputFile);
        }
      });
    }

    public Task<INode> UploadAsync(Stream stream, string name, INode parent, IProgress<double> progress)
    {
      return Task.Run(() =>
      {
        if (stream == null)
        {
          throw new ArgumentNullException("stream");
        }

        using (Stream progressionStream = new ProgressionStream(stream, progress))
        {
          return this.Upload(progressionStream, name, parent);
        }
      });
    }

    public Task<INode> UploadFileAsync(string filename, INode parent, IProgress<double> progress)
    {
      return Task.Run(() =>
      {
        using (Stream stream = new ProgressionStream(new FileStream(filename, FileMode.Open, FileAccess.Read), progress))
        {
          return this.Upload(stream, Path.GetFileName(filename), parent);
        }
      });
    }

    public Task<INodePublic> GetNodeFromLinkAsync(Uri uri)
    {
      return Task.Run(() => this.GetNodeFromLink(uri));
    }

    #endregion
  }
}
