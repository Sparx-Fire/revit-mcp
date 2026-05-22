using System.Collections.Generic;
using Newtonsoft.Json;

namespace RevitMCPKgCommandSet.Models
{
    public class KgDetectDriftEntry
    {
        [JsonProperty("revit_id", NullValueHandling = NullValueHandling.Ignore)]
        public long? RevitId { get; set; }

        [JsonProperty("llm_id", NullValueHandling = NullValueHandling.Ignore)]
        public string LlmId { get; set; }

        [JsonProperty("node_type", NullValueHandling = NullValueHandling.Ignore)]
        public string NodeType { get; set; }

        [JsonProperty("kind")]
        public string Kind { get; set; }

        [JsonProperty("kg_attrs", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, object> KgAttrs { get; set; }

        [JsonProperty("revit_attrs", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, object> RevitAttrs { get; set; }
    }

    public class KgDetectDriftResult
    {
        [JsonProperty("total_checked")]
        public int TotalChecked { get; set; }

        [JsonProperty("drift_count")]
        public int DriftCount { get; set; }

        [JsonProperty("entries")]
        public List<KgDetectDriftEntry> Entries { get; set; }
    }
}
