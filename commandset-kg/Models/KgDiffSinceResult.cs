using System.Collections.Generic;
using Newtonsoft.Json;

namespace RevitMCPKgCommandSet.Models
{
    public class KgActionLogView
    {
        [JsonProperty("turn")] public int Turn { get; set; }
        [JsonProperty("op")] public string Op { get; set; }
        [JsonProperty("target_id")] public string TargetId { get; set; }
        [JsonProperty("payload")] public Dictionary<string, object> Payload { get; set; }
    }

    public class KgDiffSinceResult
    {
        [JsonProperty("since_turn")] public int SinceTurn { get; set; }
        [JsonProperty("current_turn")] public int CurrentTurn { get; set; }
        [JsonProperty("count")] public int Count { get; set; }
        [JsonProperty("entries")] public List<KgActionLogView> Entries { get; set; }
    }
}
