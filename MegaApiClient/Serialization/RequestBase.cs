namespace CG.Web.MegaApiClient.Serialization
{
  using System.Collections.Generic;
  using Newtonsoft.Json;

  internal abstract class RequestBase
  {
    protected RequestBase(string action)
    {
      Action = action;
      QueryArguments = new Dictionary<string, string>();
      UseSession = true;
    }

    [JsonProperty("a")]
    public string Action { get; private set; }

    [JsonIgnore]
    public Dictionary<string, string> QueryArguments { get; }

    [JsonIgnore]
    public bool UseSession { get; protected set; }
  }
}
