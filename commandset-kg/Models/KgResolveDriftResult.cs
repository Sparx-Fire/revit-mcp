using System.Collections.Generic;
using Newtonsoft.Json;

namespace RevitMCPKgCommandSet.Models
{
    public class KgResolveDriftSkip
    {
        [JsonProperty("entry")]
        public KgDetectDriftEntry Entry { get; set; }

        [JsonProperty("reason")]
        public string Reason { get; set; }
    }

    public class KgResolveDriftResult
    {
        [JsonProperty("dry_run")]
        public bool DryRun { get; set; }

        [JsonProperty("total_detected")]
        public int TotalDetected { get; set; }

        [JsonProperty("total_resolved")]
        public int TotalResolved { get; set; }

        [JsonProperty("resolved_missing_in_kg")]
        public int ResolvedMissingInKg { get; set; }

        [JsonProperty("resolved_attrs_diverged")]
        public int ResolvedAttrsDiverged { get; set; }

        [JsonProperty("resolved_orphan_kg_node")]
        public int ResolvedOrphanKgNode { get; set; }

        [JsonProperty("resolved_tombstoned_but_live")]
        public int ResolvedTombstonedButLive { get; set; }

        // List of entries that were not resolved (filter excluded them OR
        // an exception occurred). Empty on a fully successful run.
        [JsonProperty("unresolved")]
        public List<KgResolveDriftSkip> Unresolved { get; set; }
            = new List<KgResolveDriftSkip>();
    }
}
