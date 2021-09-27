namespace CG.Web.MegaApiClient.Tests
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Reflection;
  using Context;
  using Newtonsoft.Json.Linq;
  using Xunit.Sdk;

  public class JsonInputsDataAttribute : DataAttribute
  {
    private static readonly JObject JsonData;

    private readonly string[] _jsonPropertyNames;
    private readonly object[] _constantArguments;

    static JsonInputsDataAttribute()
    {
      JsonData = JObject.Parse(AuthenticatedTestContext.InputsJson);
    }

    public JsonInputsDataAttribute(string jsonPropertyName)
      : this(null, new[] { jsonPropertyName })
    {
    }

    public JsonInputsDataAttribute(params string[] jsonPropertyNames)
      : this(null, jsonPropertyNames)
    {
    }

    public JsonInputsDataAttribute(object[] constantArguments, string[] jsonPropertyNames)
    {
      _constantArguments = constantArguments ?? Array.Empty<object>();
      _jsonPropertyNames = jsonPropertyNames;
    }

    public override IEnumerable<object[]> GetData(MethodInfo testMethod)
    {
      var data = _constantArguments.Concat(_jsonPropertyNames.Select(x => x == null ? null : x == string.Empty ? string.Empty : GetValue(x)));

      yield return data.ToArray();
    }

    private object GetValue(string jsonPropertyName)
    {
      JToken token = JsonData;
      var parts = jsonPropertyName.Split('.');
      foreach (var part in parts)
      {
        token = token[part];
        if (token == null)
        {
          throw new ArgumentException($"Property '{jsonPropertyName}' not found in JSON file.");
        }
      }

      return token.ToObject<object>();
    }
  }
}
