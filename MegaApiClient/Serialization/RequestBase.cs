namespace CG.Web.MegaApiClient.Serialization
{
  using System.Collections.Specialized;
  using Newtonsoft.Json;

  internal abstract class RequestBase
  {
    protected RequestBase(string action)
    {
      this.Action = action;
      this.QueryArguments = new NameValueCollection();
    }

    [JsonProperty("a")]
    public string Action { get; private set; }

    [JsonIgnore]
    public NameValueCollection QueryArguments { get; }
  }
}
