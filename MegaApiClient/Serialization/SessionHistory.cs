
namespace CG.Web.MegaApiClient.Serialization
{
  using System;
  using System.Collections.ObjectModel;
  using System.Linq;
  using System.Net;
  using Newtonsoft.Json;
  using Newtonsoft.Json.Linq;

  internal class SessionHistoryRequest : RequestBase
  {
    public SessionHistoryRequest()
      : base("usl")
    {
    }

    [JsonProperty("x")]
    public int LoadSessionIds => 1;
  }

  [JsonConverter(typeof(SessionHistoryConverter))]
  internal class SessionHistoryResponse : Collection<ISession>
  {
    internal class SessionHistoryConverter : JsonConverter
    {
      public override bool CanConvert(Type objectType)
      {
        return typeof(Session) == objectType;
      }

      public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
      {
        if (reader.TokenType == JsonToken.Null)
        {
          return null;
        }

        var response = new SessionHistoryResponse();

        var jArray = JArray.Load(reader);
        foreach (var sessionArray in jArray.OfType<JArray>())
        {
          response.Add(new Session(sessionArray));
        }

        return response;
      }

      public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
      {
        throw new NotSupportedException();
      }

      private class Session : ISession
      {
        public Session(JArray jArray)
        {
          try
          {
            LoginTime = jArray.Value<long>(0).ToDateTime();
            LastSeenTime = jArray.Value<long>(1).ToDateTime();
            Client = jArray.Value<string>(2);
            IpAddress = IPAddress.Parse(jArray.Value<string>(3));
            Country = jArray.Value<string>(4);
            SessionId = jArray.Value<string>(6);
            var isActive = jArray.Value<long>(7) == 1;
            if (jArray.Value<long>(5) == 1)
            {
              Status |= SessionStatus.Current;
            }

            if (jArray.Value<long>(7) == 1)
            {
              Status |= SessionStatus.Active;
            }

            if (Status == SessionStatus.Undefined)
            {
              Status = SessionStatus.Expired;
            }
          }
          catch (Exception ex)
          {
            Client = "Deserialization error: " + ex.Message;
          }
        }

        public string Client { get; private set; }

        public IPAddress IpAddress { get; private set; }

        public string Country { get; private set; }

        public DateTime LoginTime { get; private set; }

        public DateTime LastSeenTime { get; private set; }

        public SessionStatus Status { get; private set; }

        public string SessionId { get; private set; }
      }
    }
  }
}
