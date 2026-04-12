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

  [DebuggerDisplay("Node - Type: {Type} - Name: {Name} - Id: {Id}")]
  internal class Node : INode, INodeCrypto
  {
    private static readonly Regex s_fileAttributeRegex = new Regex(@"(?<id>\d+):(?<type>\d+)\*(?<handle>[a-zA-Z0-9-_]+)");

    private byte[] _masterKey;
    private readonly List<SharedKey> _sharedKeys;

    public Node(byte[] masterKey, ref List<SharedKey> sharedKeys)
    {
      _masterKey = masterKey;
      _sharedKeys = sharedKeys;
    }

    internal Node(string id, DownloadUrlResponse downloadResponse, byte[] key, byte[] iv, byte[] metaMac)
    {
      Id = id;
      Attributes = Crypto.DecryptAttributes(downloadResponse.SerializedAttributes.FromBase64(), key);
      Size = downloadResponse.Size;
      Type = NodeType.File;
      FileAttributes = DeserializeFileAttributes(downloadResponse.SerializedFileAttributes);
      Key = key;
      Iv = iv;
      MetaMac = metaMac;
    }

    #region Public properties

    [JsonIgnore]
    public string Name => Attributes?.Name;

    [JsonProperty("s")]
    public long Size { get; private set; }

    [JsonProperty("t")]
    public NodeType Type { get; private set; }

    [JsonProperty("h")]
    public string Id { get; private set; }

    [JsonIgnore]
    public DateTime? ModificationDate => Attributes?.ModificationDate;

    [JsonIgnore]
    public string Fingerprint => Attributes?.SerializedFingerprint;

    [JsonIgnore]
    public Attributes Attributes { get; private set; }

    [JsonProperty("p")]
    public string ParentId { get; private set; }

    [JsonIgnore]
    public DateTime? CreationDate { get; private set; }

    [JsonProperty("u")]
    public string Owner { get; private set; }

    [JsonIgnore]
    public IFileAttribute[] FileAttributes { get; private set; }

    [JsonProperty("su")]
    internal string SharingId { get; private set; }

    [JsonProperty("sk")]
    internal string SharingKey { get; private set; }

    [JsonIgnore]
    internal bool EmptyKey { get; private set; }

    #endregion

    #region INodeCrypto

    [JsonIgnore]
    public byte[] Key { get; private set; }

    [JsonIgnore]
    public byte[] SharedKey { get; private set; }

    [JsonIgnore]
    public byte[] Iv { get; private set; }

    [JsonIgnore]
    public byte[] MetaMac { get; private set; }

    [JsonIgnore]
    public byte[] FullKey { get; private set; }

    #endregion

    #region Deserialization

    [JsonProperty("ts")]
    private long SerializedCreationDate { get; set; }

    [JsonProperty("a")]
    private string SerializedAttributes { get; set; }

    [JsonProperty("k")]
    internal string SerializedKey { get; private set; }

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

        // The SerializedKey property can contain multiple keys separated with /
        // This occurs when a folder is shared and the parent is shared too, or for
        // shared folder links where the owner's key and share key are both present.
        // Try each key and use the first one that produces valid attributes.
        var serializedKeys = SerializedKey.Split('/');

        foreach (var serializedKey in serializedKeys)
        {
          var splitPosition = serializedKey.IndexOf(":", StringComparison.Ordinal);
          if (splitPosition < 0)
          {
            continue;
          }

          var handle = serializedKey.Substring(0, splitPosition);
          var encryptedKey = serializedKey.Substring(splitPosition + 1).FromBase64();

          // If node is shared, we need to retrieve shared masterkey
          var usedMasterKey = _masterKey;
          byte[] sharedKeyValue = null;
          if (_sharedKeys != null)
          {
            var sharedKey = _sharedKeys.FirstOrDefault(x => x.Id == handle);
            if (sharedKey != null)
            {
              usedMasterKey = Crypto.DecryptKey(sharedKey.Key.FromBase64(), _masterKey);
              sharedKeyValue = Type == NodeType.Directory
                ? usedMasterKey
                : Crypto.DecryptKey(encryptedKey, usedMasterKey);
            }
          }

          if (encryptedKey.Length != 16 && encryptedKey.Length != 32)
          {
            continue;
          }

          var fullKey = Crypto.DecryptKey(encryptedKey, usedMasterKey);
          byte[] nodeKey;
          byte[] iv = null;
          byte[] metaMac = null;

          if (Type == NodeType.File)
          {
            Crypto.GetPartsFromDecryptedKey(fullKey, out iv, out metaMac, out nodeKey);
          }
          else
          {
            nodeKey = fullKey;
          }

          var attrs = Crypto.DecryptAttributes(SerializedAttributes.FromBase64(), nodeKey);

          FullKey = fullKey;
          Key = nodeKey;
          Iv = iv;
          MetaMac = metaMac;
          SharedKey = sharedKeyValue;
          Attributes = attrs;

          if (attrs?.Name != null && !attrs.Name.StartsWith("Attribute deserialization failed"))
          {
            break;
          }
        }

        FileAttributes = DeserializeFileAttributes(SerializedFileAttributes);
      }
    }

    #endregion

    #region Equality

    public bool Equals(INode other)
    {
      return other != null && Id == other.Id;
    }

    public override int GetHashCode()
    {
      return Id.GetHashCode();
    }

    public override bool Equals(object obj)
    {
      return Equals(obj as INode);
    }

    #endregion

    private static IFileAttribute[] DeserializeFileAttributes(string serializedFileAttributes)
    {
      if (serializedFileAttributes == null)
      {
        return new IFileAttribute[0];
      }

      var attributes = serializedFileAttributes.Split('/');

      return attributes
        .Select(_ => s_fileAttributeRegex.Match(_))
        .Where(_ => _.Success)
        .Select(_ => new FileAttribute(
          int.Parse(_.Groups["id"].Value),
          (FileAttributeType)Enum.Parse(typeof(FileAttributeType), _.Groups["type"].Value),
          _.Groups["handle"].Value))
        .Cast<IFileAttribute>()
        .ToArray();
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

    public bool Equals(INode other)
    {
      return _node.Equals(other) && ShareId == (other as PublicNode)?.ShareId;
    }

    #region Forward

    public long Size => _node.Size;
    public string Name => _node.Name;
    public DateTime? ModificationDate => _node.ModificationDate;
    public string Fingerprint => _node.Fingerprint;
    public string Id => _node.Id;
    public string ParentId => IsShareRoot ? null : _node.ParentId;
    public string Owner => _node.Owner;
    public NodeType Type => IsShareRoot && _node.Type == NodeType.Directory ? NodeType.Root : _node.Type;
    public DateTime? CreationDate => _node.CreationDate;

    public byte[] Key => _node.Key;
    public byte[] SharedKey => _node.SharedKey;
    public byte[] Iv => _node.Iv;
    public byte[] MetaMac => _node.MetaMac;
    public byte[] FullKey => _node.FullKey;

    public IFileAttribute[] FileAttributes => _node.FileAttributes;

    #endregion

    private bool IsShareRoot
    {
      get
      {
        if (_node.SerializedKey == null)
        {
          return true;
        }

        return _node.SerializedKey.Split('/').Any(key =>
        {
          var splitPosition = key.IndexOf(":", StringComparison.Ordinal);
          return splitPosition >= 0 && key.Substring(0, splitPosition) == Id;
        });
      }
    }
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
