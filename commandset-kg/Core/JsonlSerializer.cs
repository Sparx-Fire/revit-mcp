using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RevitMCPKgCommandSet.Core
{
    public static class JsonlSerializer
    {
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore,
        };

        public static string SerializeOne(DeltaEntry entry) =>
            JsonConvert.SerializeObject(entry, Settings);

        public static DeltaEntry DeserializeOne(string line)
        {
            var entry = JsonConvert.DeserializeObject<DeltaEntry>(line);
            NormalizeAttrs(entry?.Attrs);
            NormalizeAttrs(entry?.Updates);
            return entry;
        }

        public static string SerializeAll(IEnumerable<DeltaEntry> entries)
        {
            using var sw = new StringWriter();
            foreach (var e in entries) sw.WriteLine(SerializeOne(e));
            return sw.ToString();
        }

        public static IEnumerable<DeltaEntry> DeserializeAll(string jsonl)
        {
            if (string.IsNullOrEmpty(jsonl)) yield break;
            using var sr = new StringReader(jsonl);
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                yield return DeserializeOne(line);
            }
        }

        private static void NormalizeAttrs(Dictionary<string, object> attrs)
        {
            if (attrs == null) return;
            var keys = new List<string>(attrs.Keys);
            foreach (var k in keys) attrs[k] = NormalizeValue(attrs[k]);
        }

        private static object NormalizeValue(object value)
        {
            switch (value)
            {
                case null: return null;
                case JArray arr:
                    {
                        var list = new List<object>(arr.Count);
                        foreach (var t in arr) list.Add(NormalizeValue(t));
                        return list.ToArray();
                    }
                case JObject obj:
                    {
                        var dict = new Dictionary<string, object>();
                        foreach (var prop in obj.Properties()) dict[prop.Name] = NormalizeValue(prop.Value);
                        return dict;
                    }
                case JValue jv: return jv.Value;
                default: return value;
            }
        }
    }
}
