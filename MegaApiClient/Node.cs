namespace CG.Web.MegaApiClient
{
  using System;
  using System.Collections.Generic;
  using System.Diagnostics;
  using System.Linq;
  using System.Runtime.Serialization;

  using CG.Web.MegaApiClient.Serialization;

  using Newtonsoft.Json;

  [DebuggerDisplay("NodeInfo - Type: {Type} - Name: {Name} - Id: {Id}")]
  internal class NodeInfo : INodeInfo
  {
    protected NodeInfo()
    {
    }

    internal NodeInfo(string id, DownloadUrlResponse downloadResponse, byte[] key)
    {
      this.Id = id;
      this.Attributes = Crypto.DecryptAttributes(downloadResponse.SerializedAttributes.FromBase64(), key);
      this.Size = downloadResponse.Size;
      this.Type = NodeType.File;
    }

    [JsonIgnore]
    public string Name
    {
      get { return this.Attributes?.Name; }
    }

    [JsonProperty("s")]
    public long Size { get; protected set; }

    [JsonProperty("t")]
    public NodeType Type { get; protected set; }

    [JsonProperty("h")]
    public string Id { get; private set; }

    [JsonIgnore]
    public DateTime? ModificationDate
    {
      get { return this.Attributes?.ModificationDate; }
    }

    [JsonIgnore]
    public string SerializedFingerprint
    {
      get { return this.Attributes?.SerializedFingerprint; }
    }

    [JsonIgnore]
    public Attributes Attributes { get; protected set; }

    #region Equality

    public bool Equals(INodeInfo other)
    {
      return other != null && this.Id == other.Id;
    }

    public override int GetHashCode()
    {
      return this.Id.GetHashCode();
    }

    public override bool Equals(object obj)
    {
      return this.Equals(obj as INodeInfo);
    }

    #endregion
  }

  [DebuggerDisplay("Node - Type: {Type} - Name: {Name} - Id: {Id}")]
  internal class Node : NodeInfo, INode, INodeCrypto
  {
    private byte[] masterKey;
    private List<SharedKey> sharedKeys;

    public Node(byte[] masterKey, ref List<SharedKey> sharedKeys)
    {
      this.masterKey = masterKey;
      this.sharedKeys = sharedKeys;
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
    public bool Failed { get; private set; }

    #endregion

    #region Deserialization

    [JsonProperty("ts")]
    private long SerializedCreationDate { get; set; }

    [JsonProperty("a")]
    private string SerializedAttributes { get; set; }

    [JsonProperty("k")]
    private string SerializedKey { get; set; }

    [OnDeserialized]
    public void OnDeserialized(StreamingContext ctx)
    {
      // Add key from incoming sharing.
      if (this.SharingKey != null && this.sharedKeys.Any(x => x.Id == this.Id) == false)
      {
        this.sharedKeys.Add(new SharedKey(this.Id, this.SharingKey));
      }

      this.CreationDate = this.SerializedCreationDate.ToDateTime();

      if (string.IsNullOrEmpty(this.SerializedKey))
      {
        Failed = true;

        return;
      }

      if (this.Type == NodeType.File || this.Type == NodeType.Directory)
      {
        // There are cases where the SerializedKey property contains multiple keys separated with /
        // This can occur when a folder is shared and the parent is shared too.
        // Both keys are working so we use the first one
        string serializedKey = this.SerializedKey.Split('/')[0];
        int splitPosition = serializedKey.IndexOf(":", StringComparison.Ordinal);
        byte[] encryptedKey = serializedKey.Substring(splitPosition + 1).FromBase64();

        // If node is shared, we need to retrieve shared masterkey
        if (this.sharedKeys != null)
        {
          string handle = serializedKey.Substring(0, splitPosition);
          SharedKey sharedKey = this.sharedKeys.FirstOrDefault(x => x.Id == handle);
          if (sharedKey != null)
          {
            this.masterKey = Crypto.DecryptKey(sharedKey.Key.FromBase64(), this.masterKey);
            if (this.Type == NodeType.Directory)
            {
              this.SharedKey = this.masterKey;
            }
            else
            {
              this.SharedKey = Crypto.DecryptKey(encryptedKey, this.masterKey);
            }
          }
        }

        this.FullKey = Crypto.DecryptKey(encryptedKey, this.masterKey);

        if (this.Type == NodeType.File)
        {
          byte[] iv, metaMac, fileKey;
          Crypto.GetPartsFromDecryptedKey(this.FullKey, out iv, out metaMac, out fileKey);

          this.Iv = iv;
          this.MetaMac = metaMac;
          this.Key = fileKey;
        }
        else
        {
          this.Key = this.FullKey;
        }

        this.Attributes = Crypto.DecryptAttributes(this.SerializedAttributes.FromBase64(), this.Key);
      }
    }

    #endregion

    public bool IsShareRoot
    {
      get
      {
        string serializedKey = this.SerializedKey.Split('/')[0];
        int splitPosition = serializedKey.IndexOf(":", StringComparison.Ordinal);
        return serializedKey.Substring(0, splitPosition) == this.Id;
      }
    }
  }

  [DebuggerDisplay("PublicNode - Type: {Type} - Name: {Name} - Id: {Id}")]
  internal class PublicNode : INode, INodeCrypto
  {
    private readonly Node node;

    internal PublicNode(Node node, string shareId)
    {
      this.node = node;
      this.ShareId = shareId;
    }

    public string ShareId { get; }

    public bool Equals(INodeInfo other)
    {
      return this.node.Equals(other) && this.ShareId == (other as PublicNode)?.ShareId;
    }

    #region Forward

    public long Size { get { return this.node.Size; } }
    public string Name { get { return this.node.Name; } }
    public DateTime? ModificationDate { get { return this.node.ModificationDate; } }
    public string SerializedFingerprint { get { return this.node.Attributes.SerializedFingerprint; } }
    public string Id { get { return this.node.Id; } }
    public string ParentId { get { return this.node.IsShareRoot ? null : this.node.ParentId; } }
    public string Owner { get { return this.node.Owner; } }
    public NodeType Type { get { return this.node.IsShareRoot && this.node.Type == NodeType.Directory ? NodeType.Root : this.node.Type; } }
    public DateTime CreationDate { get { return this.node.CreationDate; } }

    public byte[] Key { get { return this.node.Key; } }
    public byte[] SharedKey { get { return this.node.SharedKey; } }
    public byte[] Iv { get { return this.node.Iv; } }
    public byte[] MetaMac { get { return this.node.MetaMac; } }
    public byte[] FullKey { get { return this.node.FullKey; } }

    #endregion
  }
}
