using System;

namespace CG.Web.MegaApiClient
{
    public interface INode : IEquatable<INode>
    {
        string Id { get; }

        string ParentId { get; }

        string Owner { get; }

        NodeType Type { get; }

        long Size { get; }

        string Name { get; }

        DateTime LastModificationDate { get; }
    }

    internal interface INodeCrypto
    { 
        byte[] DecryptedKey { get; }

        byte[] Key { get; }

        byte[] Iv { get; }

        byte[] MetaMac { get; }
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