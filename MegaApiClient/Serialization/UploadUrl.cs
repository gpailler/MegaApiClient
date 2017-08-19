namespace CG.Web.MegaApiClient.Serialization
{
  using Newtonsoft.Json;

  internal class UploadUrlRequest : RequestBase
  {
    public UploadUrlRequest(long fileSize)
      : base("u")
    {
      this.Size = fileSize;
    }

    [JsonProperty("s")]
    public long Size { get; private set; }
  }

  internal class UploadUrlResponse
  {
    [JsonProperty("p")]
    public string Url { get; private set; }
  }
}
