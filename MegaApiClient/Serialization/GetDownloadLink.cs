namespace CG.Web.MegaApiClient.Serialization
{
  using Newtonsoft.Json;

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
}
