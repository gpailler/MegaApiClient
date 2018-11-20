namespace CG.Web.MegaApiClient.Serialization
{
  using Newtonsoft.Json;

  internal class PreLoginRequest : RequestBase
  {
    public PreLoginRequest(string userHandle)
      : base("us0")
    {
      this.UserHandle = userHandle;
    }

    [JsonProperty("user")]
    public string UserHandle { get; private set; }
  }

  internal class PreLoginResponse
  {
    [JsonProperty("s")]
    public string Salt { get; private set; }

    [JsonProperty("v")]
    public int Version { get; private set; }
  }
}
