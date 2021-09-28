namespace CG.Web.MegaApiClient.Tests
{
  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.Threading;
  using System.Threading.Tasks;

  public class MegaApiClientAsyncWrapper : IMegaApiClient, IDisposable
  {
    private readonly IMegaApiClient _client;

    public MegaApiClientAsyncWrapper(IMegaApiClient client)
    {
      _client = client;
      _client.ApiRequestFailed += OnApiRequestFailed;
    }

    public void Dispose()
    {
      _client.ApiRequestFailed -= OnApiRequestFailed;
    }

    public event EventHandler<ApiRequestFailedEventArgs> ApiRequestFailed;

    public bool IsLoggedIn => _client.IsLoggedIn;

    public MegaApiClient.LogonSessionToken Login(string email, string password)
    {
      return UnwrapException(() => _client.LoginAsync(email, password).Result);
    }

    public MegaApiClient.LogonSessionToken Login(string email, string password, string mfaKey)
    {
      return UnwrapException(() => _client.LoginAsync(email, password, mfaKey).Result);
    }

    public MegaApiClient.LogonSessionToken Login(MegaApiClient.AuthInfos authInfos)
    {
      return UnwrapException(() => _client.LoginAsync(authInfos).Result);
    }

    public void Login(MegaApiClient.LogonSessionToken logonSessionToken)
    {
      UnwrapException(() => _client.LoginAsync(logonSessionToken).Wait());
    }

    public void Login()
    {
      UnwrapException(() => _client.LoginAsync().Wait());
    }

    public void LoginAnonymous()
    {
      UnwrapException(() => _client.LoginAnonymousAsync().Wait());
    }

    public void Logout()
    {
      UnwrapException(() => _client.LogoutAsync().Wait());
    }

    public string GetRecoveryKey()
    {
      return UnwrapException(() => _client.GetRecoveryKeyAsync().Result);
    }

    public IAccountInformation GetAccountInformation()
    {
      return UnwrapException(() => _client.GetAccountInformationAsync().Result);
    }

    public IEnumerable<ISession> GetSessionsHistory()
    {
      return UnwrapException(() => _client.GetSessionsHistoryAsync().Result);
    }

    public IEnumerable<INode> GetNodes()
    {
      return UnwrapException(() => _client.GetNodesAsync().Result);
    }

    public IEnumerable<INode> GetNodes(INode parent)
    {
      return UnwrapException(() => _client.GetNodesAsync(parent).Result);
    }

    public void Delete(INode node, bool moveToTrash)
    {
      UnwrapException(() => _client.DeleteAsync(node, moveToTrash).Wait());
    }

    public INode CreateFolder(string name, INode parent)
    {
      return UnwrapException(() => _client.CreateFolderAsync(name, parent).Result);
    }

    public Uri GetDownloadLink(INode node)
    {
      return UnwrapException(() => _client.GetDownloadLinkAsync(node).Result);
    }

    public void DownloadFile(INode node, string outputFile, CancellationToken? cancellationToken = null)
    {
      var progress = new Progress<double>();
      UnwrapException(() => _client.DownloadFileAsync(node, outputFile, progress, cancellationToken).Wait());
    }

    public void DownloadFile(Uri uri, string outputFile, CancellationToken? cancellationToken = null)
    {
      var progress = new Progress<double>();
      UnwrapException(() => _client.DownloadFileAsync(uri, outputFile, progress, cancellationToken).Wait());
    }

    public Stream Download(INode node, CancellationToken? cancellationToken = null)
    {
      var progress = new Progress<double>();
      return UnwrapException(() => _client.DownloadAsync(node, progress, cancellationToken).Result);
    }

    public Stream Download(Uri uri, CancellationToken? cancellationToken = null)
    {
      var progress = new Progress<double>();
      return UnwrapException(() => _client.DownloadAsync(uri, progress, cancellationToken).Result);
    }

    public INode GetNodeFromLink(Uri uri)
    {
      return UnwrapException(() => _client.GetNodeFromLinkAsync(uri).Result);
    }

    public IEnumerable<INode> GetNodesFromLink(Uri uri)
    {
      return UnwrapException(() => _client.GetNodesFromLinkAsync(uri).Result);
    }

    public INode UploadFile(string filename, INode parent, CancellationToken? cancellationToken = null)
    {
      var progress = new Progress<double>();
      return UnwrapException(() => _client.UploadFileAsync(filename, parent, progress, cancellationToken).Result);
    }

    public INode Upload(Stream stream, string name, INode parent, DateTime? modificationDate = null, CancellationToken? cancellationToken = null)
    {
      var progress = new Progress<double>();
      return UnwrapException(() => _client.UploadAsync(stream, name, parent, progress, modificationDate).Result);
    }

    public INode Move(INode node, INode destinationParentNode)
    {
      return UnwrapException(() => _client.MoveAsync(node, destinationParentNode).Result);
    }

    public INode Rename(INode node, string newName)
    {
      return UnwrapException(() => _client.RenameAsync(node, newName).Result);
    }

    public MegaApiClient.AuthInfos GenerateAuthInfos(string email, string password)
    {
      return UnwrapException(() => _client.GenerateAuthInfosAsync(email, password).Result);
    }

    public MegaApiClient.AuthInfos GenerateAuthInfos(string email, string password, string mfaKey)
    {
      return UnwrapException(() => _client.GenerateAuthInfosAsync(email, password, mfaKey).Result);
    }

    public Stream DownloadFileAttribute(INode node, FileAttributeType fileAttributeType, CancellationToken? cancellationToken = null)
    {
      return UnwrapException(() => _client.DownloadFileAttributeAsync(node, fileAttributeType, cancellationToken).Result);
    }

    public Task<MegaApiClient.LogonSessionToken> LoginAsync(string email, string password)
    {
      return _client.LoginAsync(email, password);
    }

    public Task<MegaApiClient.LogonSessionToken> LoginAsync(string email, string password, string mfaKey)
    {
      return _client.LoginAsync(email, password, mfaKey);
    }

    public Task<MegaApiClient.LogonSessionToken> LoginAsync(MegaApiClient.AuthInfos authInfos)
    {
      return _client.LoginAsync(authInfos);
    }

    public Task LoginAsync(MegaApiClient.LogonSessionToken authInfos)
    {
      return _client.LoginAsync(authInfos);
    }

    public Task LoginAsync()
    {
      return _client.LoginAsync();
    }

    public Task LoginAnonymousAsync()
    {
      return _client.LoginAnonymousAsync();
    }

    public Task LogoutAsync()
    {
      return _client.LogoutAsync();
    }

    public Task<string> GetRecoveryKeyAsync()
    {
      return _client.GetRecoveryKeyAsync();
    }

    public Task<IAccountInformation> GetAccountInformationAsync()
    {
      return _client.GetAccountInformationAsync();
    }

    public Task<IEnumerable<ISession>> GetSessionsHistoryAsync()
    {
      return _client.GetSessionsHistoryAsync();
    }

    public Task<IEnumerable<INode>> GetNodesAsync()
    {
      return _client.GetNodesAsync();
    }

    public Task<IEnumerable<INode>> GetNodesAsync(INode parent)
    {
      return _client.GetNodesAsync(parent);
    }

    public Task<INode> CreateFolderAsync(string name, INode parent)
    {
      return _client.CreateFolderAsync(name, parent);
    }

    public Task DeleteAsync(INode node, bool moveToTrash = true)
    {
      return _client.DeleteAsync(node, moveToTrash);
    }

    public Task<INode> MoveAsync(INode sourceNode, INode destinationParentNode)
    {
      return _client.MoveAsync(sourceNode, destinationParentNode);
    }

    public Task<INode> RenameAsync(INode sourceNode, string newName)
    {
      return _client.RenameAsync(sourceNode, newName);
    }

    public Task<Uri> GetDownloadLinkAsync(INode node)
    {
      return _client.GetDownloadLinkAsync(node);
    }

    public Task<Stream> DownloadAsync(INode node, IProgress<double> progress = null, CancellationToken? cancellationToken = null)
    {
      return _client.DownloadAsync(node, progress, cancellationToken);
    }

    public Task<Stream> DownloadAsync(Uri uri, IProgress<double> progress = null, CancellationToken? cancellationToken = null)
    {
      return _client.DownloadAsync(uri, progress, cancellationToken);
    }

    public Task DownloadFileAsync(INode node, string outputFile, IProgress<double> progress = null, CancellationToken? cancellationToken = null)
    {
      return _client.DownloadFileAsync(node, outputFile, progress, cancellationToken);
    }

    public Task DownloadFileAsync(Uri uri, string outputFile, IProgress<double> progress = null, CancellationToken? cancellationToken = null)
    {
      return _client.DownloadFileAsync(uri, outputFile, progress, cancellationToken);
    }

    public Task<INode> UploadFileAsync(string filename, INode parent, IProgress<double> progress = null, CancellationToken? cancellationToken = null)
    {
      return _client.UploadFileAsync(filename, parent, progress, cancellationToken);
    }

    public Task<INode> UploadAsync(Stream stream, string name, INode parent, IProgress<double> progress = null, DateTime? modificationDate = null, CancellationToken? cancellationToken = null)
    {
      return _client.UploadAsync(stream, name, parent, progress, modificationDate, cancellationToken);
    }

    public Task<INode> GetNodeFromLinkAsync(Uri uri)
    {
      return _client.GetNodeFromLinkAsync(uri);
    }

    public Task<IEnumerable<INode>> GetNodesFromLinkAsync(Uri uri)
    {
      return _client.GetNodesFromLinkAsync(uri);
    }

    public Task<MegaApiClient.AuthInfos> GenerateAuthInfosAsync(string email, string password)
    {
      return _client.GenerateAuthInfosAsync(email, password);
    }

    public Task<MegaApiClient.AuthInfos> GenerateAuthInfosAsync(string email, string password, string mfaKey)
    {
      return _client.GenerateAuthInfosAsync(email, password, mfaKey);
    }

    public Task<Stream> DownloadFileAttributeAsync(INode node, FileAttributeType fileAttributeType, CancellationToken? cancellationToken = null)
    {
      return _client.DownloadFileAttributeAsync(node, fileAttributeType, cancellationToken);
    }

    private static T UnwrapException<T>(Func<T> action)
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

    private static void UnwrapException(Action action)
    {
      UnwrapException(
        () =>
        {
          action();
          return true;
        });
    }

    private void OnApiRequestFailed(object sender, ApiRequestFailedEventArgs e)
    {
      ApiRequestFailed?.Invoke(sender, e);
    }
  }
}
