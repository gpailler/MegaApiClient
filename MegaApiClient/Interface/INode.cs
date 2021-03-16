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

    string SerializedFingerprint { get; }
  }

  public interface INode : INodeInfo
  {
    string ParentId { get; }

    DateTime CreationDate { get; }

    string Owner { get; }

    IFileAttribute[] FileAttributes { get; }
  }

  internal interface INodeCrypto
  {
    byte[] Key { get; }

    byte[] SharedKey { get; }

    byte[] Iv { get; }

    byte[] MetaMac { get; }

    byte[] FullKey { get; }
  }

  public interface IFileAttribute
  {
    int Id { get; }

    FileAttributeType Type { get; }

    string Handle { get; }
  }

  public enum FileAttributeType
  {
    Thumbnail = 0,
    Preview = 1
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
