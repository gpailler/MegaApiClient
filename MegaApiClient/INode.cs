namespace CG.Web.MegaApiClient
{
  using System;

  public interface INodePublic
  {
    long Size { get; }

    string Name { get; }

    DateTime ModificationDate { get; }
  }

  public interface INode : INodePublic, IEquatable<INode>
  {
      string Id { get; }
      NodeType Type { get; }

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