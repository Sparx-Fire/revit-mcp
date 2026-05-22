using System.Collections.Generic;
using Newtonsoft.Json;

namespace RevitMCPKgCommandSet.Models
{
    public class KgTraverseReachedNode
    {
        [JsonProperty("llm_id")] public string LlmId { get; set; }
        [JsonProperty("node_type")] public string NodeType { get; set; }
        // BFS depth from start (reachability mode only; omitted in fixed-path mode).
        [JsonProperty("depth", NullValueHandling = NullValueHandling.Ignore)] public int? Depth { get; set; }
        // Whether the reached node is tombstoned (reachability mode).
        [JsonProperty("soft_deleted", NullValueHandling = NullValueHandling.Ignore)] public bool? SoftDeleted { get; set; }
    }

    public class KgTraverseResult
    {
        [JsonProperty("start_id")] public string StartId { get; set; }
        [JsonProperty("mode")] public string Mode { get; set; }   // "path" | "reachable"
        [JsonProperty("step_count", NullValueHandling = NullValueHandling.Ignore)] public int? StepCount { get; set; }
        [JsonProperty("max_depth", NullValueHandling = NullValueHandling.Ignore)] public int? MaxDepth { get; set; }
        [JsonProperty("count")] public int Count { get; set; }
        [JsonProperty("reached")] public List<KgTraverseReachedNode> Reached { get; set; }
    }
}
