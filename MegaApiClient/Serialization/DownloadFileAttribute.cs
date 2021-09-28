namespace CG.Web.MegaApiClient.Serialization
{
  using Newtonsoft.Json;

  internal class DownloadFileAttributeRequest : RequestBase
  {
    public DownloadFileAttributeRequest(string fileAttributeHandle)
      : base("ufa")
    {
      Id = fileAttributeHandle;
    }

    [JsonProperty("ssl")]
    public int Ssl => 2;

    [JsonProperty("r")]
    public int R => 1;

    [JsonProperty("fah")]
    public string Id { get; private set; }
  }

  internal class DownloadFileAttributeResponse
  {
    [JsonProperty("p")]
    public string Url { get; private set; }
  }
}
