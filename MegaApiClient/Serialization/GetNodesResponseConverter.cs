namespace CG.Web.MegaApiClient.Serialization
{
  using System;
  using System.Collections.Generic;
  using Newtonsoft.Json;
  using Newtonsoft.Json.Linq;

  internal class GetNodesResponseConverter : JsonConverter
  {
    private readonly byte[] masterKey;

    public GetNodesResponseConverter(byte[] masterKey)
    {
      this.masterKey = masterKey;
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

      JObject jObject = JObject.Load(reader);

      GetNodesResponse target = new GetNodesResponse(this.masterKey);

      JsonReader jObjectReader = jObject.CreateReader();
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
