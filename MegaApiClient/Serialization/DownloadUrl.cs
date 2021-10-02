namespace CG.Web.MegaApiClient.Serialization
{
  using Newtonsoft.Json;

  internal class DownloadUrlRequest : RequestBase
  {
    public DownloadUrlRequest(INode node)
      : base("g")
    {
      Id = node.Id;

      if (node is PublicNode publicNode)
      {
        QueryArguments["n"] = publicNode.ShareId;
      }
    }

    [JsonProperty("g")]
    public int G => 1;

    [JsonProperty("n")]
    public string Id { get; private set; }
  }

  internal class DownloadUrlRequestFromId : RequestBase
  {
    public DownloadUrlRequestFromId(string id)
      : base("g")
    {
      Id = id;
    }

    [JsonProperty("g")]
    public int G => 1;

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

    [JsonProperty("fa")]
    public string SerializedFileAttributes { get; set; }
  }
}
