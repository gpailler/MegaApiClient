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
      }
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
}
