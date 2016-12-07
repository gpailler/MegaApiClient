namespace CG.Web.MegaApiClient
{
  using System;
  using System.Collections.Generic;
  using System.Diagnostics;
  using System.Linq;
  using System.Runtime.Serialization;

  using Newtonsoft.Json;
  using Newtonsoft.Json.Linq;

  #region Base

  internal abstract class RequestBase
  {
    protected RequestBase(string action)
    {
      this.Action = action;
    }

    [JsonProperty("a")]
    public string Action { get; private set; }
  }

  #endregion


  #region Login

  internal class LoginRequest : RequestBase
  {
    public LoginRequest(string userHandle, string passwordHash)
      : base("us")
    {
      this.UserHandle = userHandle;
      this.PasswordHash = passwordHash;
    }

    [JsonProperty("user")]
    public string UserHandle { get; private set; }

    [JsonProperty("uh")]
    public string PasswordHash { get; private set; }
  }

  internal class LoginResponse
  {
    [JsonProperty("csid")]
    public string SessionId { get; private set; }

    [JsonProperty("tsid")]
    public string TemporarySessionId { get; private set; }

    [JsonProperty("privk")]
    public string PrivateKey { get; private set; }

    [JsonProperty("k")]
    public string MasterKey { get; private set; }
  }

  internal class AnonymousLoginRequest : RequestBase
  {
    public AnonymousLoginRequest(string masterKey, string temporarySession)
      : base("up")
    {
      this.MasterKey = masterKey;
      this.TemporarySession = temporarySession;
    }

    [JsonProperty("k")]
    public string MasterKey { get; set; }

    [JsonProperty("ts")]
    public string TemporarySession { get; set; }
  }

    internal class LogoutRequest : RequestBase
    {
        public LogoutRequest()
            : base("sml")
        { }
    }

  #endregion

  #region AccountInformation

  internal class AccountInformationRequest : RequestBase
  {
    public AccountInformationRequest()
      : base("uq")
    {
    }

    [JsonProperty("strg")]
    public int Storage { get { return 1; } }

    [JsonProperty("xfer")]
    public int Transfer { get { return 0; } }

    [JsonProperty("pro")]
    public int AccountType { get { return 0; } }
  }

  internal class AccountInformationResponse : IAccountInformation
  {
    [JsonProperty("mstrg")]
    public long TotalQuota { get; private set; }

    [JsonProperty("cstrg")]
    public long UsedQuota { get; private set; }

    [JsonProperty("cstrgn")]
    public Dictionary<string, int[]> StorageMetrics {get; private set;}
  }

  #endregion


  #region Nodes

  internal class GetNodesRequest : RequestBase
  {
    public GetNodesRequest()
      : base("f")
    {
      this.c = 1;
    }

    public int c { get; private set; }
  }

  internal class GetNodesResponse
  {
    public Node[] Nodes { get; private set; }

    [JsonProperty("f")]
    public JRaw NodesSerialized { get; private set; }

    [JsonProperty("ok")]
    public List<SharedKey> SharedKeys { get; private set; }

    [OnDeserialized]
    public void OnDeserialized(StreamingContext ctx)
    {
      JsonSerializerSettings settings = new JsonSerializerSettings();

      // First Nodes deserialization to retrieve all shared keys
      settings.Context = new StreamingContext(StreamingContextStates.All, new[] { this });
      JsonConvert.DeserializeObject<Node[]>(this.NodesSerialized.ToString(), settings);

      // Deserialize nodes
      settings.Context = new StreamingContext(StreamingContextStates.All, new[] { this, ctx.Context });
      this.Nodes = JsonConvert.DeserializeObject<Node[]>(this.NodesSerialized.ToString(), settings);
    }
  }

  #endregion


  #region Delete

  internal class DeleteRequest : RequestBase
  {
    public DeleteRequest(INode node)
      : base("d")
    {
      this.Node = node.Id;
    }

    [JsonProperty("n")]
    public string Node { get; private set; }
  }

  #endregion


  #region Link

  internal class GetDownloadLinkRequest : RequestBase
  {
    public GetDownloadLinkRequest(INode node)
      : base("l")
    {
      this.Id = node.Id;
    }

    [JsonProperty("n")]
    public string Id { get; private set; }
  }

  #endregion


  #region Create node

  internal class CreateNodeRequest : RequestBase
  {
    private CreateNodeRequest(INode parentNode, NodeType type, string attributes, string encryptedKey, byte[] key, string completionHandle, long? lastModificationTime = null)
      : base("p")
    {
      this.ParentId = parentNode.Id;
      this.Nodes = new[]
      {
        new CreateNodeRequestData
        {
            Attributes = attributes,
            Key = encryptedKey,
            Type = type,
            CompletionHandle = completionHandle,
            ModificationDate = lastModificationTime
        }
      };

      INodeCrypto parentNodeCrypto = parentNode as INodeCrypto;
      if (parentNodeCrypto == null)
      {
        throw new ArgumentException("parentNode node must implement INodeCrypto");
      }

      if (parentNodeCrypto.SharedKey != null)
      {
        this.Share = new ShareData(parentNode.Id);
        this.Share.AddItem(completionHandle, key, parentNodeCrypto.SharedKey);
      }
    }

    [JsonProperty("t")]
    public string ParentId { get; private set; }

    [JsonProperty("cr")]
    public ShareData Share { get; private set; }

    [JsonProperty("n")]
    public CreateNodeRequestData[] Nodes { get; private set; }

    public static CreateNodeRequest CreateFileNodeRequest(INode parentNode, string attributes, string encryptedkey, byte[] fileKey, string completionHandle, long? lastModificationTime = null)
    {
      return new CreateNodeRequest(parentNode, NodeType.File, attributes, encryptedkey, fileKey, completionHandle, lastModificationTime);
    }

    public static CreateNodeRequest CreateFolderNodeRequest(INode parentNode, string attributes, string encryptedkey, byte[] key, long? lastModificationTime = null)
    {
      return new CreateNodeRequest(parentNode, NodeType.Directory, attributes, encryptedkey, key, "xxxxxxxx", lastModificationTime);
    }

    internal class CreateNodeRequestData
    {
      [JsonProperty("h")]
      public string CompletionHandle { get; set; }

      [JsonProperty("t")]
      public NodeType Type { get; set; }

      [JsonProperty("a")]
      public string Attributes { get; set; }

      [JsonProperty("k")]
      public string Key { get; set; }

        [JsonProperty("ts", DefaultValueHandling = DefaultValueHandling.Ignore)]
      public long? ModificationDate { get; set; }
    }
  }

  #endregion


  #region ShareRequest

  internal class ShareNodeRequest : RequestBase
  {
    public ShareNodeRequest(INode node, byte[] masterKey, IEnumerable<INode> nodes)
      : base("s2")
    {
      this.Id = node.Id;
      this.Options = new object[] { new { r = 0, u = "EXP" } };

      INodeCrypto nodeCrypto = (INodeCrypto)node;
      byte[] uncryptedSharedKey = nodeCrypto.SharedKey;
      if (uncryptedSharedKey == null)
      {
        uncryptedSharedKey = Crypto.CreateAesKey();
      }

      this.SharedKey = Crypto.EncryptKey(uncryptedSharedKey, masterKey).ToBase64();

      if (nodeCrypto.SharedKey == null)
      {
        this.Share = new ShareData(node.Id);

        this.Share.AddItem(node.Id, nodeCrypto.FullKey, uncryptedSharedKey);

        // Add all children
        IEnumerable<INode> allChildren = this.GetRecursiveChildren(nodes.ToArray(), node);
        foreach (var child in allChildren)
        {
          this.Share.AddItem(child.Id, ((INodeCrypto)child).FullKey, uncryptedSharedKey);
        }
      }

      byte[] handle = (node.Id + node.Id).ToBytes();
      this.HandleAuth = Crypto.EncryptKey(handle, masterKey).ToBase64();
    }

    private IEnumerable<INode> GetRecursiveChildren(INode[] nodes, INode parent)
    {
      foreach (var node in nodes.Where(x => x.Type == NodeType.Directory || x.Type == NodeType.File))
      {
        string parentId = node.Id;
        do
        {
          parentId = nodes.FirstOrDefault(x => x.Id == parentId).ParentId;
          if (parentId == parent.Id)
          {
            yield return node;
            break;
          }
        } while (parentId != null);
      }
    }

    [JsonProperty("n")]
    public string Id { get; private set; }

    [JsonProperty("ha")]
    public string HandleAuth { get; private set; }

    [JsonProperty("s")]
    public object[] Options { get; private set; }

    [JsonProperty("cr")]
    public ShareData Share { get; private set; }

    [JsonProperty("ok")]
    public string SharedKey { get; private set; }
  }

  #endregion


  #region UploadRequest

  internal class UploadUrlRequest : RequestBase
  {
    public UploadUrlRequest(long fileSize)
      : base("u")
    {
      this.Size = fileSize;
    }

    [JsonProperty("s")]
    public long Size { get; private set; }
  }

  internal class UploadUrlResponse
  {
    [JsonProperty("p")]
    public string Url { get; private set; }
  }

  #endregion


  #region DownloadRequest

  internal class DownloadUrlRequest : RequestBase
  {
    public DownloadUrlRequest(INode node)
      : base("g")
    {
      this.Id = node.Id;
    }

    public int g { get { return 1; } }

    [JsonProperty("n")]
    public string Id { get; private set; }
  }

  internal class DownloadUrlRequestFromId : RequestBase
  {
    public DownloadUrlRequestFromId(string id)
      : base("g")
    {
      this.Id = id;
    }

    public int g { get { return 1; } }

    [JsonProperty("p")]
    public string Id { get; private set; }
  }

  internal class DownloadUrlResponse
  {
    [JsonProperty("g")]
    public string Url { get; private set; }

    [JsonProperty("s")]
    public long Size { get; private set; }

    [JsonProperty("at")]
    public string SerializedAttributes { get; set; }
  }

  #endregion


  #region Move

  internal class MoveRequest : RequestBase
  {
    public MoveRequest(INode node, INode destinationParentNode)
      : base("m")
    {
      this.Id = node.Id;
      this.DestinationParentId = destinationParentNode.Id;
    }

    [JsonProperty("n")]
    public string Id { get; private set; }

    [JsonProperty("t")]
    public string DestinationParentId { get; private set; }
  }

  #endregion

  #region Rename

  internal class RenameRequest : RequestBase
  {
    public RenameRequest(INode node, string attributes)
      : base("a")
    {
      this.Id = node.Id;
      this.SerializedAttributes = attributes;
    }

    [JsonProperty("n")]
    public string Id { get; private set; }

    [JsonProperty("attr")]
    public string SerializedAttributes { get; set; }
  }

  #endregion


  #region Attributes


  internal class Attributes
  {
      public Attributes()
      { }

      public Attributes(string name)
      {
          this.Name = name;
      }

      public Attributes(string name, uint[] crc, DateTime? modificationDate = null)
      {
          this.Name = name;
          CRC = crc;
          ModificationDate = modificationDate;
      }

      [JsonProperty("n")]
      public string Name { get; private set; }
      
     [JsonProperty("c", DefaultValueHandling = DefaultValueHandling.Ignore)]
     public string FingerprintBase64 { get; private set; }

     [JsonIgnore]
     public DateTime? ModificationDate
     {
         get;
         private set;
     }

     [JsonIgnore]
     public uint[] CRC
     {
         get;
         private set;
     }
      
      public bool ParseFingerprint(FileFingerprint? fileFingerprintRef = null)
      {
          if (this.FingerprintBase64 == null)
              return false;
          var fingerprintBytes = this.FingerprintBase64.FromBase64();

          FileFingerprint fileFingerprint = fileFingerprintRef.HasValue ? fileFingerprintRef.Value : default(FileFingerprint);
          
          if (!fileFingerprint.UnserializeFingerprint(fingerprintBytes))
              return false;

          ulong modificationDateSeconds = fileFingerprint.ModificationTimeStamp;
          CRC = fileFingerprint.CRC;

          ModificationDate = Node.OriginalDateTime.AddSeconds(modificationDateSeconds).ToLocalTime();

          return true;
      }

      public bool SetFingerprint(ref FileFingerprint fileFingerprint)
      {
          byte[] fingerprintBytes = fileFingerprint.SerializeFingerprint();
          string fingerprintBase64Encoded = Convert.ToBase64String(fingerprintBytes);
          FingerprintBase64 = fingerprintBase64Encoded;
          return true;
      }
  }

  #endregion

  #region ShareData

  [JsonConverter(typeof(ShareDataConverter))]
  internal class ShareData
  {
    private IList<ShareDataItem> items;

    public ShareData(string nodeId)
    {
      this.NodeId = nodeId;
      this.items = new List<ShareDataItem>();
    }

    public string NodeId { get; private set; }

    public IEnumerable<ShareDataItem> Items { get { return this.items; } }

    public void AddItem(string nodeId, byte[] data, byte[] key)
    {
      ShareDataItem item = new ShareDataItem
      {
        NodeId = nodeId,
        Data = data,
        Key = key
      };

      this.items.Add(item);
    }

    public class ShareDataItem
    {
      public string NodeId { get; set; }

      public byte[] Data { get; set; }

      public byte[] Key { get; set; }
    }
  }

  internal class ShareDataConverter : JsonConverter
  {
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
      ShareData data = value as ShareData;
      if (data == null)
      {
        throw new ArgumentException("invalid data to serialize");
      }

      writer.WriteStartArray();

      writer.WriteStartArray();
      writer.WriteValue(data.NodeId);
      writer.WriteEndArray();

      writer.WriteStartArray();
      foreach (var item in data.Items)
      {
        writer.WriteValue(item.NodeId);
      }
      writer.WriteEndArray();

      writer.WriteStartArray();
      int counter = 0;
      foreach (var item in data.Items)
      {
        writer.WriteValue(0);
        writer.WriteValue(counter++);
        writer.WriteValue(Crypto.EncryptKey(item.Data, item.Key).ToBase64());
      }
      writer.WriteEndArray();

      writer.WriteEndArray();
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
      throw new NotImplementedException();
    }

    public override bool CanConvert(Type objectType)
    {
      return objectType == typeof(ShareData);
    }
  }

  [DebuggerDisplay("Id: {Id} / Key: {Key}")]
  internal class SharedKey
  {
    public SharedKey(string id, string key)
    {
      this.Id = id;
      this.Key = key;
    }

    [JsonProperty("h")]
    public string Id { get; private set; }

    [JsonProperty("k")]
    public string Key { get; private set; }
  }

  #endregion
}
