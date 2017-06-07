namespace CG.Web.MegaApiClient.Serialization
{
  using Newtonsoft.Json;

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
}
