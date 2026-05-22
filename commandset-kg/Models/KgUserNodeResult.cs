using Newtonsoft.Json;

namespace RevitMCPKgCommandSet.Models
{
    // Shared result for the user-node authoring commands
    // (kg_create_node / kg_modify_node / kg_delete_node).
    public class KgUserNodeResult
    {
        [JsonProperty("operation")] public string Operation { get; set; }
        [JsonProperty("llm_id")] public string LlmId { get; set; }
        [JsonProperty("node_type")] public string NodeType { get; set; }
        [JsonProperty("turn")] public int Turn { get; set; }
    }
}
