﻿namespace CG.Web.MegaApiClient.Serialization
{
  using Newtonsoft.Json;

  internal class AnonymousLoginRequest : RequestBase
  {
    public AnonymousLoginRequest(string masterKey, string temporarySession)
      : base("up")
    {
      MasterKey = masterKey;
      TemporarySession = temporarySession;
    }

    [JsonProperty("k")]
    public string MasterKey { get; set; }

    [JsonProperty("ts")]
    public string TemporarySession { get; set; }
  }
}
