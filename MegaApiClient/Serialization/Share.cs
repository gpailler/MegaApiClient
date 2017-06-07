namespace CG.Web.MegaApiClient.Serialization
{
  using System;
  using System.Collections.Generic;
  using System.Diagnostics;
  using Newtonsoft.Json;

  [JsonConverter(typeof(ShareDataConverter))]
  internal class ShareData
  {
    private IList<ShareDataItem> items;

    public ShareData(string nodeId)
    {
      this.NodeId = nodeId;
      this.items = new List<ShareDataItem>();
    }

    public string NodeId { get; private set; }

    public IEnumerable<ShareDataItem> Items { get { return this.items; } }

    public void AddItem(string nodeId, byte[] data, byte[] key)
    {
      ShareDataItem item = new ShareDataItem
      {
        NodeId = nodeId,
        Data = data,
        Key = key
      };

      this.items.Add(item);
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
      ShareData data = value as ShareData;
      if (data == null)
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
      int counter = 0;
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
      this.Id = id;
      this.Key = key;
    }

    [JsonProperty("h")]
    public string Id { get; private set; }

    [JsonProperty("k")]
    public string Key { get; private set; }
  }
}
