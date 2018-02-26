namespace CG.Web.MegaApiClient.Serialization
{
  using System.Collections.Generic;
  using System.Runtime.Serialization;
  using Newtonsoft.Json;
  using Newtonsoft.Json.Linq;

  internal class GetNodesRequest : RequestBase
  {
    public GetNodesRequest(string shareId = null)
      : base("f")
    {
      this.c = 1;

      if (shareId != null)
      {
        this.QueryArguments["n"] = shareId;

        // Retrieve all nodes in all subfolders
        this.r = 1;
      }
    }

    public int c { get; private set; }
    public int r { get; private set; }
  }

  internal class GetNodesResponse
  {
    private readonly byte[] masterKey;
    private List<SharedKey> sharedKeys;

    public GetNodesResponse(byte[] masterKey)
    {
      this.masterKey = masterKey;
    }

    public Node[] Nodes { get; private set; }

    [JsonProperty("f")]
    public JRaw NodesSerialized { get; private set; }

    [JsonProperty("ok")]
    public List<SharedKey> SharedKeys
    {
      get { return this.sharedKeys; }
      private set { this.sharedKeys = value; }
    }

    [OnDeserialized]
    public void OnDeserialized(StreamingContext ctx)
    {
      this.Nodes = JsonConvert.DeserializeObject<Node[]>(this.NodesSerialized.ToString(), new NodeConverter(this.masterKey, ref this.sharedKeys));
    }
  }
}
