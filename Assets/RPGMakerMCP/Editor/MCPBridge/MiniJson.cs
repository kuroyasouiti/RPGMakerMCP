using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MCP.Editor
{
    /// <summary>
    /// JSON encoder/decoder wrapper using Newtonsoft.Json.
    /// Provides compatibility layer for existing code while using robust JSON library.
    /// </summary>
    public static class MiniJson
    {
        private static readonly JsonSerializerSettings DeserializeSettings = new JsonSerializerSettings
        {
            DateParseHandling = DateParseHandling.None,
            FloatParseHandling = FloatParseHandling.Double
        };

        private static readonly JsonSerializerSettings SerializeSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Include,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        /// <summary>
        /// Deserialize JSON string to object (Dictionary or List).
        /// </summary>
        public static object Deserialize(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            var token = JsonConvert.DeserializeObject<JToken>(json, DeserializeSettings);
            return ConvertJToken(token);
        }

        /// <summary>
        /// Serialize object to JSON string.
        /// </summary>
        public static string Serialize(object obj)
        {
            return JsonConvert.SerializeObject(obj, SerializeSettings);
        }

        /// <summary>
        /// Serialize object to JSON string with custom formatting.
        /// </summary>
        public static string Serialize(object obj, bool indented)
        {
            var settings = new JsonSerializerSettings
            {
                Formatting = indented ? Formatting.Indented : Formatting.None,
                NullValueHandling = NullValueHandling.Include,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };
            return JsonConvert.SerializeObject(obj, settings);
        }

        /// <summary>
        /// Convert JToken to standard .NET types for compatibility.
        /// </summary>
        private static object ConvertJToken(JToken token)
        {
            if (token == null)
            {
                return null;
            }

            switch (token.Type)
            {
                case JTokenType.Object:
                    var dict = new Dictionary<string, object>();
                    foreach (var prop in ((JObject)token).Properties())
                    {
                        dict[prop.Name] = ConvertJToken(prop.Value);
                    }
                    return dict;

                case JTokenType.Array:
                    var list = new List<object>();
                    foreach (var item in (JArray)token)
                    {
                        list.Add(ConvertJToken(item));
                    }
                    return list;

                case JTokenType.Integer:
                    var longValue = token.Value<long>();
                    // Return int if within int range for compatibility
                    if (longValue >= int.MinValue && longValue <= int.MaxValue)
                    {
                        return (int)longValue;
                    }
                    return longValue;

                case JTokenType.Float:
                    return token.Value<double>();

                case JTokenType.String:
                    return token.Value<string>();

                case JTokenType.Boolean:
                    return token.Value<bool>();

                case JTokenType.Null:
                case JTokenType.Undefined:
                    return null;

                default:
                    return token.ToString();
            }
        }
    }
}
