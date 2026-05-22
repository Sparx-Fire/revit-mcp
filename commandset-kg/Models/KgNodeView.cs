using System.Collections.Generic;
using Newtonsoft.Json;

namespace RevitMCPKgCommandSet.Models
{
    public class KgNodeView
    {
        [JsonProperty("llm_id")] public string LlmId { get; set; }
        [JsonProperty("node_type")] public string NodeType { get; set; }
        [JsonProperty("revit_id")] public long? RevitId { get; set; }
        [JsonProperty("created_at_turn")] public int CreatedAtTurn { get; set; }
        [JsonProperty("modified_at_turn")] public List<int> ModifiedAtTurn { get; set; }
        [JsonProperty("deleted_at_turn")] public int? DeletedAtTurn { get; set; }
        [JsonProperty("attrs")] public Dictionary<string, object> Attrs { get; set; }
    }

    public class KgEdgeView
    {
        [JsonProperty("src")] public string Src { get; set; }
        [JsonProperty("dst")] public string Dst { get; set; }
        [JsonProperty("edge_type")] public string EdgeType { get; set; }
    }
}
