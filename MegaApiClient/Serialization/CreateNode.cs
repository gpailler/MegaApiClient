namespace CG.Web.MegaApiClient.Serialization
{
  using System;
  using Newtonsoft.Json;

  internal class CreateNodeRequest : RequestBase
  {
    private CreateNodeRequest(INode parentNode, NodeType type, string attributes, string encryptedKey, byte[] key, string completionHandle)
      : base("p")
    {
      ParentId = parentNode.Id;
      Nodes = new[]
      {
        new CreateNodeRequestData
        {
          Attributes = attributes,
          Key = encryptedKey,
          Type = type,
          CompletionHandle = completionHandle
        }
      };

      if (!(parentNode is INodeCrypto parentNodeCrypto))
      {
        throw new ArgumentException("parentNode node must implement INodeCrypto");
      }

      if (parentNodeCrypto.SharedKey != null)
      {
        Share = new ShareData(parentNode.Id);
        Share.AddItem(completionHandle, key, parentNodeCrypto.SharedKey);
      }
    }

    [JsonProperty("t")]
    public string ParentId { get; private set; }

    [JsonProperty("cr")]
    public ShareData Share { get; private set; }

    [JsonProperty("n")]
    public CreateNodeRequestData[] Nodes { get; private set; }

    public static CreateNodeRequest CreateFileNodeRequest(INode parentNode, string attributes, string encryptedkey, byte[] fileKey, string completionHandle)
    {
      return new CreateNodeRequest(parentNode, NodeType.File, attributes, encryptedkey, fileKey, completionHandle);
    }

    public static CreateNodeRequest CreateFolderNodeRequest(INode parentNode, string attributes, string encryptedkey, byte[] key)
    {
      return new CreateNodeRequest(parentNode, NodeType.Directory, attributes, encryptedkey, key, "xxxxxxxx");
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
    }
  }
}
