namespace CG.Web.MegaApiClient.Serialization
{
  using Newtonsoft.Json;

  internal class LoginRequest : RequestBase
  {
    public LoginRequest(string userHandle, string passwordHash)
      : base("us")
    {
      this.UserHandle = userHandle;
      this.PasswordHash = passwordHash;
    }

    [JsonProperty("user")]
    public string UserHandle { get; private set; }

    [JsonProperty("uh")]
    public string PasswordHash { get; private set; }
  }

  internal class LoginResponse
  {
    [JsonProperty("csid")]
    public string SessionId { get; private set; }

    [JsonProperty("tsid")]
    public string TemporarySessionId { get; private set; }

    [JsonProperty("privk")]
    public string PrivateKey { get; private set; }

    [JsonProperty("k")]
    public string MasterKey { get; private set; }
  }
}
