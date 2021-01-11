namespace CG.Web.MegaApiClient.Serialization
{
  using Newtonsoft.Json;

  internal class DownloadFileAttributeRequest : RequestBase
  {
    public DownloadFileAttributeRequest(string fileAttributeHandle)
      : base("ufa")
    {
      this.Id = fileAttributeHandle;
    }

    public int ssl { get { return 2; } }

    public int r { get { return 1; } }

    [JsonProperty("fah")]
    public string Id { get; private set; }
  }

  internal class DownloadFileAttributeResponse
  {
    [JsonProperty("p")]
    public string Url { get; private set; }
  }
}
