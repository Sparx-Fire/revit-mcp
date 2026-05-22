using Newtonsoft.Json;

namespace RevitMCPKgCommandSet.Models
{
    public class KgAnnotateResult
    {
        [JsonProperty("operation")] public string Operation { get; set; }
        [JsonProperty("src")] public string Src { get; set; }
        [JsonProperty("dst")] public string Dst { get; set; }
        [JsonProperty("kind")] public string Kind { get; set; }
        [JsonProperty("turn")] public int Turn { get; set; }
    }
}
