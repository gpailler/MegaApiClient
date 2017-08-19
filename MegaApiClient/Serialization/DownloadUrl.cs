namespace CG.Web.MegaApiClient.Serialization
{
  using Newtonsoft.Json;

  internal class DownloadUrlRequest : RequestBase
  {
    public DownloadUrlRequest(INode node)
      : base("g")
    {
      this.Id = node.Id;

      PublicNode publicNode = node as PublicNode;
      if (publicNode != null)
      {
        this.QueryArguments["n"] = publicNode.ShareId;
      }
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
}
