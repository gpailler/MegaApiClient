
namespace CG.Web.MegaApiClient
{
  using System;
  using System.Collections.Generic;
  using System.Diagnostics;
  using System.Linq;
  using System.Runtime.Serialization;
  using System.Text.RegularExpressions;
  using Cryptography;
  using Newtonsoft.Json;
  using Serialization;

  [DebuggerDisplay("NodeInfo - Type: {Type} - Name: {Name} - Id: {Id}")]
  internal class NodeInfo : INodeInfo
  {
    protected NodeInfo()
    {
    }

    internal NodeInfo(string id, DownloadUrlResponse downloadResponse, byte[] key)
    {
      Id = id;
      Attributes = Crypto.DecryptAttributes(downloadResponse.SerializedAttributes.FromBase64(), key);
      Size = downloadResponse.Size;
      Type = NodeType.File;
    }

    [JsonIgnore]
    public string Name => Attributes?.Name;

    [JsonProperty("s")]
    public long Size { get; protected set; }

    [JsonProperty("t")]
    public NodeType Type { get; protected set; }

    [JsonProperty("h")]
    public string Id { get; private set; }

    [JsonIgnore]
    public DateTime? ModificationDate => Attributes?.ModificationDate;

    [JsonIgnore]
    public string SerializedFingerprint => Attributes?.SerializedFingerprint;

    [JsonIgnore]
    public Attributes Attributes { get; protected set; }

    #region Equality

    public bool Equals(INodeInfo other)
    {
      return other != null && Id == other.Id;
    }

    public override int GetHashCode()
    {
      return Id.GetHashCode();
    }

    public override bool Equals(object obj)
    {
      return Equals(obj as INodeInfo);
    }

    #endregion
  }

  [DebuggerDisplay("Node - Type: {Type} - Name: {Name} - Id: {Id}")]
  internal class Node : NodeInfo, INode, INodeCrypto
  {
    private static readonly Regex s_fileAttributeRegex = new Regex(@"(?<id>\d+):(?<type>\d+)\*(?<handle>[a-zA-Z0-9-_]+)");

    private byte[] _masterKey;
    private readonly List<SharedKey> _sharedKeys;

    public Node(byte[] masterKey, ref List<SharedKey> sharedKeys)
    {
      _masterKey = masterKey;
      _sharedKeys = sharedKeys;
    }

    #region Public properties

    [JsonProperty("p")]
    public string ParentId { get; private set; }

    [JsonProperty("u")]
    public string Owner { get; private set; }

    [JsonProperty("su")]
    public string SharingId { get; set; }

    [JsonProperty("sk")]
    public string SharingKey { get; set; }

    [JsonIgnore]
    public DateTime CreationDate { get; private set; }

    [JsonIgnore]
    public byte[] Key { get; private set; }

    [JsonIgnore]
    public byte[] FullKey { get; private set; }

    [JsonIgnore]
    public byte[] SharedKey { get; private set; }

    [JsonIgnore]
    public byte[] Iv { get; private set; }

    [JsonIgnore]
    public byte[] MetaMac { get; private set; }

    [JsonIgnore]
    public bool EmptyKey { get; private set; }

    [JsonIgnore]
    public IFileAttribute[] FileAttributes { get; private set; }

    #endregion

    #region Deserialization

    [JsonProperty("ts")]
    private long SerializedCreationDate { get; set; }

    [JsonProperty("a")]
    private string SerializedAttributes { get; set; }

    [JsonProperty("k")]
    private string SerializedKey { get; set; }

    [JsonProperty("fa")]
    private string SerializedFileAttributes { get; set; }

    [OnDeserialized]
    public void OnDeserialized(StreamingContext ctx)
    {
      // Add key from incoming sharing.
      if (SharingKey != null && _sharedKeys.Any(x => x.Id == Id) == false)
      {
        _sharedKeys.Add(new SharedKey(Id, SharingKey));
      }

      CreationDate = SerializedCreationDate.ToDateTime();

      if (Type == NodeType.File || Type == NodeType.Directory)
      {
        // Check if file is not yet decrypted
        if (string.IsNullOrEmpty(SerializedKey))
        {
          EmptyKey = true;

          return;
        }

        // There are cases where the SerializedKey property contains multiple keys separated with /
        // This can occur when a folder is shared and the parent is shared too.
        // Both keys are working so we use the first one
        var serializedKey = SerializedKey.Split('/')[0];
        var splitPosition = serializedKey.IndexOf(":", StringComparison.Ordinal);
        var encryptedKey = serializedKey.Substring(splitPosition + 1).FromBase64();

        // If node is shared, we need to retrieve shared masterkey
        if (_sharedKeys != null)
        {
          var handle = serializedKey.Substring(0, splitPosition);
          var sharedKey = _sharedKeys.FirstOrDefault(x => x.Id == handle);
          if (sharedKey != null)
          {
            _masterKey = Crypto.DecryptKey(sharedKey.Key.FromBase64(), _masterKey);
            if (Type == NodeType.Directory)
            {
              SharedKey = _masterKey;
            }
            else
            {
              SharedKey = Crypto.DecryptKey(encryptedKey, _masterKey);
            }
          }
        }

        FullKey = Crypto.DecryptKey(encryptedKey, _masterKey);

        if (Type == NodeType.File)
        {
          Crypto.GetPartsFromDecryptedKey(FullKey, out var iv, out var metaMac, out var fileKey);

          Iv = iv;
          MetaMac = metaMac;
          Key = fileKey;
        }
        else
        {
          Key = FullKey;
        }

        Attributes = Crypto.DecryptAttributes(SerializedAttributes.FromBase64(), Key);

        if (SerializedFileAttributes != null)
        {
          var attributes = SerializedFileAttributes.Split('/');
          FileAttributes = attributes
            .Select(_ => s_fileAttributeRegex.Match(_))
            .Where(_ => _.Success)
            .Select(_ => new FileAttribute(
              int.Parse(_.Groups["id"].Value),
              (FileAttributeType)Enum.Parse(typeof(FileAttributeType), _.Groups["type"].Value),
              _.Groups["handle"].Value))
            .ToArray();
        }
      }
    }

    #endregion

    public bool IsShareRoot
    {
      get
      {
        var serializedKey = SerializedKey.Split('/')[0];
        var splitPosition = serializedKey.IndexOf(":", StringComparison.Ordinal);
        return serializedKey.Substring(0, splitPosition) == Id;
      }
    }
  }

  [DebuggerDisplay("PublicNode - Type: {Type} - Name: {Name} - Id: {Id}")]
  internal class PublicNode : INode, INodeCrypto
  {
    private readonly Node _node;

    internal PublicNode(Node node, string shareId)
    {
      _node = node;
      ShareId = shareId;
    }

    public string ShareId { get; }

    public bool Equals(INodeInfo other)
    {
      return _node.Equals(other) && ShareId == (other as PublicNode)?.ShareId;
    }

    #region Forward

    public long Size => _node.Size;
    public string Name => _node.Name;
    public DateTime? ModificationDate => _node.ModificationDate;
    public string SerializedFingerprint => _node.Attributes.SerializedFingerprint;
    public string Id => _node.Id;
    public string ParentId => _node.IsShareRoot ? null : _node.ParentId;
    public string Owner => _node.Owner;
    public NodeType Type => _node.IsShareRoot && _node.Type == NodeType.Directory ? NodeType.Root : _node.Type;
    public DateTime CreationDate => _node.CreationDate;

    public byte[] Key => _node.Key;
    public byte[] SharedKey => _node.SharedKey;
    public byte[] Iv => _node.Iv;
    public byte[] MetaMac => _node.MetaMac;
    public byte[] FullKey => _node.FullKey;

    public IFileAttribute[] FileAttributes => _node.FileAttributes;

    #endregion
  }

  internal class FileAttribute : IFileAttribute
  {
    public FileAttribute(int id, FileAttributeType type, string handle)
    {
      Id = id;
      Type = type;
      Handle = handle;
    }

    public int Id { get; }
    public FileAttributeType Type { get; }
    public string Handle { get; }
  }
}
