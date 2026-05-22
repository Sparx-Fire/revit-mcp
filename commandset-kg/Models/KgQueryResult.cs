using System.Collections.Generic;
using Newtonsoft.Json;

namespace RevitMCPKgCommandSet.Models
{
    public class KgQueryResult
    {
        [JsonProperty("count")] public int Count { get; set; }

        // Omitted when an aggregation is requested (the agent asked for a
        // scalar/grouped summary, not the node list).
        [JsonProperty("nodes", NullValueHandling = NullValueHandling.Ignore)]
        public List<KgNodeView> Nodes { get; set; }

        // Present only when params carried an `aggregate` spec. Lets the
        // agent get count/sum/mean/min/max (optionally grouped) server-side
        // instead of fetching every node and computing in-context.
        [JsonProperty("aggregate", NullValueHandling = NullValueHandling.Ignore)]
        public KgAggregateResult Aggregate { get; set; }

        // Present only when params carried a `join` spec (edge-aware
        // projection). One flat row per matched node, with the chained
        // neighbours' projected attrs — a relational audit in one call.
        [JsonProperty("rows", NullValueHandling = NullValueHandling.Ignore)]
        public List<Dictionary<string, object>> Rows { get; set; }
    }

    public class KgAggregateResult
    {
        [JsonProperty("op")] public string Op { get; set; }
        [JsonProperty("field", NullValueHandling = NullValueHandling.Ignore)] public string Field { get; set; }
        [JsonProperty("group_by", NullValueHandling = NullValueHandling.Ignore)] public string GroupBy { get; set; }

        // Scalar result (when no group_by). int for count, double for sum/mean/min/max.
        [JsonProperty("value", NullValueHandling = NullValueHandling.Ignore)] public object Value { get; set; }

        // Per-group results (when group_by is set).
        [JsonProperty("groups", NullValueHandling = NullValueHandling.Ignore)] public List<KgAggGroup> Groups { get; set; }

        // Number of nodes that fed the aggregation.
        [JsonProperty("n")] public int N { get; set; }
    }

    public class KgAggGroup
    {
        [JsonProperty("key")] public object Key { get; set; }
        [JsonProperty("value")] public object Value { get; set; }
        [JsonProperty("n")] public int N { get; set; }
    }
}
