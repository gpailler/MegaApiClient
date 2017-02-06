namespace CG.Web.MegaApiClient.Tests
{
  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.Threading;
  using System.Threading.Tasks;

  public class MegaApiClientAsyncWrapper : IMegaApiClient, IDisposable
  {
    private readonly IMegaApiClient client;

    public MegaApiClientAsyncWrapper(IMegaApiClient client)
    {
      this.client = client;
      this.client.ApiRequestFailed += this.OnApiRequestFailed;
    }

    public void Dispose()
    {
      this.client.ApiRequestFailed -= this.OnApiRequestFailed;
    }

    public event EventHandler<ApiRequestFailedEventArgs> ApiRequestFailed;

    public bool IsLoggedIn
    {
      get { return this.client.IsLoggedIn; }
    }

    public MegaApiClient.LogonSessionToken Login(string email, string password)
    {
      return this.UnwrapException(() => this.client.LoginAsync(email, password).Result);
    }

    public MegaApiClient.LogonSessionToken Login(MegaApiClient.AuthInfos authInfos)
    {
      return this.UnwrapException(() => this.client.LoginAsync(authInfos).Result);
    }

    public void Login(MegaApiClient.LogonSessionToken logonSessionToken)
    {
      this.UnwrapException(() => this.client.LoginAsync(logonSessionToken).Wait());
    }

    public void LoginAnonymous()
    {
      this.UnwrapException(() => this.client.LoginAnonymousAsync().Wait());
    }

    public void Logout()
    {
      this.UnwrapException(() => this.client.LogoutAsync().Wait());
    }

    public IAccountInformation GetAccountInformation()
    {
      return this.UnwrapException(() => this.client.GetAccountInformationAsync().Result);
    }

    public IEnumerable<INode> GetNodes()
    {
      return this.UnwrapException(() => this.client.GetNodesAsync().Result);
    }

    public IEnumerable<INode> GetNodes(INode parent)
    {
      return this.UnwrapException(() => this.client.GetNodesAsync(parent).Result);
    }

    public void Delete(INode node, bool moveToTrash)
    {
      this.UnwrapException(() => this.client.DeleteAsync(node, moveToTrash).Wait());
    }

    public INode CreateFolder(string name, INode parent)
    {
      return this.UnwrapException(() => this.client.CreateFolderAsync(name, parent).Result);
    }

    public Uri GetDownloadLink(INode node)
    {
      return this.UnwrapException(() => this.client.GetDownloadLinkAsync(node).Result);
    }

    public void DownloadFile(INode node, string outputFile, CancellationToken? cancellationToken = null)
    {
      Progress<double> progress = new Progress<double>();
      this.UnwrapException(() => this.client.DownloadFileAsync(node, outputFile, progress, cancellationToken).Wait());
    }

    public void DownloadFile(Uri uri, string outputFile, CancellationToken? cancellationToken = null)
    {
      Progress<double> progress = new Progress<double>();
      this.UnwrapException(() => this.client.DownloadFileAsync(uri, outputFile, progress, cancellationToken).Wait());
    }

    public Stream Download(IDownloadNode node, CancellationToken? cancellationToken = null)
    {
      Progress<double> progress = new Progress<double>();
      return this.UnwrapException(() => this.client.DownloadAsync(node, progress, cancellationToken).Result);
    }

    public Stream Download(Uri uri, CancellationToken? cancellationToken = null)
    {
      Progress<double> progress = new Progress<double>();
      return this.UnwrapException(() => this.client.DownloadAsync(uri, progress, cancellationToken).Result);
    }

    public IDownloadNode GetNodeFromLink(Uri uri)
    {
      return this.UnwrapException(() => this.client.GetNodeFromLinkAsync(uri).Result);
    }

    public IEnumerable<INode> GetNodesFromLink(Uri uri)
    {
      return this.UnwrapException(() => this.client.GetNodesFromLinkAsync(uri).Result);
    }

    public INode UploadFile(string filename, INode parent, CancellationToken? cancellationToken = null)
    {
      Progress<double> progress = new Progress<double>();
      return this.UnwrapException(() => this.client.UploadFileAsync(filename, parent, progress, cancellationToken).Result);
    }

    public INode Upload(Stream stream, string name, INode parent, DateTime? modificationDate = null, CancellationToken? cancellationToken = null)
    {
      Progress<double> progress = new Progress<double>();
      return this.UnwrapException(() => this.client.UploadAsync(stream, name, parent, progress, modificationDate).Result);
    }

    public INode Move(INode node, INode destinationParentNode)
    {
      return this.UnwrapException(() => this.client.MoveAsync(node, destinationParentNode).Result);
    }

    public INode Rename(INode node, string newName)
    {
      return this.UnwrapException(() => this.client.RenameAsync(node, newName).Result);
    }

    public Task<MegaApiClient.LogonSessionToken> LoginAsync(string email, string password)
    {
      throw new NotImplementedException();
    }

    public Task<MegaApiClient.LogonSessionToken> LoginAsync(MegaApiClient.AuthInfos authInfos)
    {
      throw new NotImplementedException();
    }

    public Task LoginAsync(MegaApiClient.LogonSessionToken authInfos)
    {
      throw new NotImplementedException();
    }

    public Task LoginAnonymousAsync()
    {
      throw new NotImplementedException();
    }

    public Task LogoutAsync()
    {
      throw new NotImplementedException();
    }

    public Task<IAccountInformation> GetAccountInformationAsync()
    {
      throw new NotImplementedException();
    }

    public Task<IEnumerable<INode>> GetNodesAsync()
    {
      throw new NotImplementedException();
    }

    public Task<IEnumerable<INode>> GetNodesAsync(INode parent)
    {
      throw new NotImplementedException();
    }

    public Task<INode> CreateFolderAsync(string name, INode parent)
    {
      throw new NotImplementedException();
    }

    public Task DeleteAsync(INode node, bool moveToTrash = true)
    {
      throw new NotImplementedException();
    }

    public Task<INode> MoveAsync(INode sourceNode, INode destinationParentNode)
    {
      throw new NotImplementedException();
    }

    public Task<INode> RenameAsync(INode sourceNode, string newName)
    {
      throw new NotImplementedException();
    }

    public Task<Uri> GetDownloadLinkAsync(INode node)
    {
      throw new NotImplementedException();
    }

    public Task<Stream> DownloadAsync(IDownloadNode node, IProgress<double> progress, CancellationToken? cancellationToken = null)
    {
      throw new NotImplementedException();
    }

    public Task<Stream> DownloadAsync(Uri uri, IProgress<double> progress, CancellationToken? cancellationToken = null)
    {
      throw new NotImplementedException();
    }

    public Task DownloadFileAsync(INode node, string outputFile, IProgress<double> progress, CancellationToken? cancellationToken = null)
    {
      return this.client.DownloadFileAsync(node, outputFile, progress, cancellationToken);
    }

    public Task DownloadFileAsync(Uri uri, string outputFile, IProgress<double> progress, CancellationToken? cancellationToken = null)
    {
      return this.client.DownloadFileAsync(uri, outputFile, progress, cancellationToken);
    }

    public Task<INode> UploadFileAsync(string filename, INode parent, IProgress<double> progress, CancellationToken? cancellationToken = null)
    {
      return this.client.UploadFileAsync(filename, parent, progress, cancellationToken);
    }

    public Task<INode> UploadAsync(Stream stream, string name, INode parent, IProgress<double> progress, DateTime? modificationDate = null, CancellationToken? cancellationToken = null)
    {
      return this.client.UploadAsync(stream, name, parent, progress, modificationDate, cancellationToken);
    }

    public Task<IDownloadNode> GetNodeFromLinkAsync(Uri uri)
    {
      throw new NotImplementedException();
    }

    public Task<IEnumerable<INode>> GetNodesFromLinkAsync(Uri uri)
    {
      throw new NotImplementedException();
    }

    private T UnwrapException<T>(Func<T> action)
    {
      try
      {
        return action();
      }
      catch (AggregateException ex)
      {
        throw ex.InnerException;
      }
    }

    private void UnwrapException(Action action)
    {
      this.UnwrapException(
        () =>
        {
          action();
          return true;
        });
    }

    private void OnApiRequestFailed(object sender, ApiRequestFailedEventArgs e)
    {
      this.ApiRequestFailed?.Invoke(sender, e);
    }
  }
}
