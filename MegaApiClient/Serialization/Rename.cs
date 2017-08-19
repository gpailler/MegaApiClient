namespace CG.Web.MegaApiClient.Serialization
{
  using Newtonsoft.Json;

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
}
