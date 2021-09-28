﻿namespace CG.Web.MegaApiClient.Serialization
{
  using System;
  using System.Collections.Generic;
  using Newtonsoft.Json;
  using Newtonsoft.Json.Linq;

  internal class NodeConverter : JsonConverter
  {
    private readonly byte[] _masterKey;
    private List<SharedKey> _sharedKeys;

    public NodeConverter(byte[] masterKey, ref List<SharedKey> sharedKeys)
    {
      _masterKey = masterKey;
      _sharedKeys = sharedKeys;
    }

    public override bool CanConvert(Type objectType)
    {
      return typeof(Node) == objectType;
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
      if (reader.TokenType == JsonToken.Null)
      {
        return null;
      }

      var jObject = JObject.Load(reader);

      var target = new Node(_masterKey, ref _sharedKeys);

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
