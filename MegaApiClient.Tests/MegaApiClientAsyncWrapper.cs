// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MegaApiClientAsyncWrapper.cs" company="DxO Labs">
//   Copyright DxO Labs 2016 - All Rights Reserved
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace CG.Web.MegaApiClient.Tests
{
  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.Threading.Tasks;

  public class MegaApiClientAsyncWrapper : IMegaApiClient
  {
    private readonly IMegaApiClient client;

    public MegaApiClientAsyncWrapper(IMegaApiClient client)
    {
      this.client = client;
    }

    public int BufferSize
    {
      get { return this.client.BufferSize; }
      set { this.client.BufferSize = value; }
    }

    public int ChunksPackSize
    {
      get { return this.client.ChunksPackSize; }
      set { this.client.ChunksPackSize = value; }
    }

    public void Login(string email, string password)
    {
      this.UnwrapException(() => this.client.LoginAsync(email, password).Wait());
    }

    public void Login(MegaApiClient.AuthInfos authInfos)
    {
      this.UnwrapException(() => this.client.LoginAsync(authInfos).Wait());
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

    public void DownloadFile(INode node, string outputFile)
    {
      Progress<double> progress = new Progress<double>();
      this.UnwrapException(() => this.client.DownloadFileAsync(node, outputFile, progress).Wait());
    }

    public void DownloadFile(Uri uri, string outputFile)
    {
      Progress<double> progress = new Progress<double>();
      this.UnwrapException(() => this.client.DownloadFileAsync(uri, outputFile, progress).Wait());
    }

    public Stream Download(INode node)
    {
      Progress<double> progress = new Progress<double>();
      return this.UnwrapException(() => this.client.DownloadAsync(node, progress).Result);
    }

    public Stream Download(Uri uri)
    {
      Progress<double> progress = new Progress<double>();
      return this.UnwrapException(() => this.client.DownloadAsync(uri, progress).Result);
    }

    public INodePublic GetNodeFromLink(Uri uri)
    {
      return this.UnwrapException(() => this.client.GetNodeFromLinkAsync(uri).Result);
    }

    public INode UploadFile(string filename, INode parent)
    {
      Progress<double> progress = new Progress<double>();
      return this.UnwrapException(() => this.client.UploadFileAsync(filename, parent, progress).Result);
    }

    public INode Upload(Stream stream, string name, INode parent)
    {
      Progress<double> progress = new Progress<double>();
      return this.UnwrapException(() => this.client.UploadAsync(stream, name, parent, progress).Result);
    }

    public INode Move(INode node, INode destinationParentNode)
    {
      return this.UnwrapException(() => this.client.MoveAsync(node, destinationParentNode).Result);
    }

    public INode Rename(INode node, string newName)
    {
      return this.UnwrapException(() => this.client.RenameAsync(node, newName).Result);
    }

    public Task LoginAsync(string email, string password)
    {
      throw new NotImplementedException();
    }

    public Task LoginAsync(MegaApiClient.AuthInfos authInfos)
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

    public Task<Stream> DownloadAsync(INode node, IProgress<double> progress)
    {
      throw new NotImplementedException();
    }

    public Task<Stream> DownloadAsync(Uri uri, IProgress<double> progress)
    {
      throw new NotImplementedException();
    }

    public Task DownloadFileAsync(INode node, string outputFile, IProgress<double> progress)
    {
      return this.client.DownloadFileAsync(node, outputFile, progress);
    }

    public Task DownloadFileAsync(Uri uri, string outputFile, IProgress<double> progress)
    {
      return this.client.DownloadFileAsync(uri, outputFile, progress);
    }

    public Task<INode> UploadFileAsync(string filename, INode parent, IProgress<double> progress)
    {
      return this.client.UploadFileAsync(filename, parent, progress);
    }

    public Task<INode> UploadAsync(Stream stream, string name, INode parent, IProgress<double> progress)
    {
      return this.client.UploadAsync(stream, name, parent, progress);
    }

    public Task<INodePublic> GetNodeFromLinkAsync(Uri uri)
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
  }
}
