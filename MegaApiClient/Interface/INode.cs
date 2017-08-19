namespace CG.Web.MegaApiClient
{
  using System;

  public interface INodeInfo : IEquatable<INodeInfo>
  {
    string Id { get; }

    NodeType Type { get; }

    string Name { get; }

    long Size { get; }

    DateTime? ModificationDate { get; }
  }

  public interface INode : INodeInfo
  {
    string ParentId { get; }

    DateTime CreationDate { get; }

    string Owner { get; }
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