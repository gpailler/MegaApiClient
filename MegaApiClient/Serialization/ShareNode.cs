namespace CG.Web.MegaApiClient.Serialization
{
  using System.Collections.Generic;
  using System.Linq;
  using Newtonsoft.Json;

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
          parentId = nodes.FirstOrDefault(x => x.Id == parentId)?.ParentId;
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
}
