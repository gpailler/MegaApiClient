namespace CG.Web.MegaApiClient.Serialization
{
  using System;
  using Newtonsoft.Json;
  using Newtonsoft.Json.Linq;

  internal class GetNodesResponseConverter : JsonConverter
  {
    private readonly byte[] _masterKey;

    public GetNodesResponseConverter(byte[] masterKey)
    {
      _masterKey = masterKey;
    }

    public override bool CanConvert(Type objectType)
    {
      return typeof(GetNodesResponse) == objectType;
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
      if (reader.TokenType == JsonToken.Null)
      {
        return null;
      }

      var jObject = JObject.Load(reader);

      var target = new GetNodesResponse(_masterKey);

      var jObjectReader = jObject.CreateReader();
      jObjectReader.Culture = reader.Culture;
      jObjectReader.DateFormatString = reader.DateFormatString;
      jObjectReader.DateParseHandling = reader.DateParseHandling;
      jObjectReader.DateTimeZoneHandling = reader.DateTimeZoneHandling;
      jObjectReader.FloatParseHandling = reader.FloatParseHandling;
      jObjectReader.MaxDepth = reader.MaxDepth;
      jObjectReader.SupportMultipleContent = reader.SupportMultipleContent;
      serializer.Populate(jObjectReader, target);

      return target;
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
      throw new NotSupportedException();
    }
  }
}
