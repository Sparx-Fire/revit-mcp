using Newtonsoft.Json;

namespace RevitMCPKgCommandSet.Models
{
    public class KgGetByRevitIdResult
    {
        [JsonProperty("found")] public bool Found { get; set; }
        [JsonProperty("node")] public KgNodeView Node { get; set; }
    }
}
