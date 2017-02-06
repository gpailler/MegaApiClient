namespace CG.Web.MegaApiClient
{
  using System;

  public interface IDownloadNode : IEquatable<INode>
  {
    long Size { get; }

    string Name { get; }

    DateTime? ModificationDate { get; }

    string Id { get; }

    NodeType Type { get; }
  }

  public interface INode : IDownloadNode
  {
    string ParentId { get; }

    string Owner { get; }

    DateTime CreationDate { get; }
  }

  internal interface INodeCrypto
  {
    byte[] Key { get; }

    byte[] SharedKey { get; }

    byte[] Iv { get; }

    byte[] MetaMac { get; }

    byte[] FullKey { get; }
  }

  public enum NodeType
  {
    File = 0,
    Directory,
    Root,
    Inbox,
    Trash
  }
}