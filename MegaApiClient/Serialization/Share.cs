namespace CG.Web.MegaApiClient.Serialization
{
  using System;
  using System.Collections.Generic;
  using System.Diagnostics;
  using CG.Web.MegaApiClient.Cryptography;
  using Newtonsoft.Json;

  [JsonConverter(typeof(ShareDataConverter))]
  internal class ShareData
  {
    private readonly IList<ShareDataItem> _items;

    public ShareData(string nodeId)
    {
      NodeId = nodeId;
      _items = new List<ShareDataItem>();
    }

    public string NodeId { get; private set; }

    public IEnumerable<ShareDataItem> Items => _items;

    public void AddItem(string nodeId, byte[] data, byte[] key)
    {
      var item = new ShareDataItem
      {
        NodeId = nodeId,
        Data = data,
        Key = key
      };

      _items.Add(item);
    }

    public class ShareDataItem
    {
      public string NodeId { get; set; }

      public byte[] Data { get; set; }

      public byte[] Key { get; set; }
    }
  }

  internal class ShareDataConverter : JsonConverter
  {
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
      if (!(value is ShareData data))
      {
        throw new ArgumentException("invalid data to serialize");
      }

      writer.WriteStartArray();

      writer.WriteStartArray();
      writer.WriteValue(data.NodeId);
      writer.WriteEndArray();

      writer.WriteStartArray();
      foreach (var item in data.Items)
      {
        writer.WriteValue(item.NodeId);
      }

      writer.WriteEndArray();

      writer.WriteStartArray();
      var counter = 0;
      foreach (var item in data.Items)
      {
        writer.WriteValue(0);
        writer.WriteValue(counter++);
        writer.WriteValue(Crypto.EncryptKey(item.Data, item.Key).ToBase64());
      }

      writer.WriteEndArray();

      writer.WriteEndArray();
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
      throw new NotImplementedException();
    }

    public override bool CanConvert(Type objectType)
    {
      return objectType == typeof(ShareData);
    }
  }

  [DebuggerDisplay("Id: {Id} / Key: {Key}")]
  internal class SharedKey
  {
    public SharedKey(string id, string key)
    {
      Id = id;
      Key = key;
    }

    [JsonProperty("h")]
    public string Id { get; private set; }

    [JsonProperty("k")]
    public string Key { get; private set; }
  }
}
