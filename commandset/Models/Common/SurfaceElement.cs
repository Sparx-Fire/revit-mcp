using Newtonsoft.Json;

namespace RevitMCPCommandSet.Models.Common;

/// <summary>
/// </summary>
public class SurfaceElement
{
    public SurfaceElement()
    {
        Parameters = new Dictionary<string, double>();
    }

    /// <summary>
    /// </summary>
    [JsonProperty("category")]
    public string Category { get; set; } = "INVALID";

    /// <summary>
    /// </summary>
    [JsonProperty("typeId")]
    public int TypeId { get; set; } = -1;

    /// <summary>
    /// </summary>
    [JsonProperty("boundary")]
    public JZFace Boundary { get; set; }

    /// <summary>
    /// </summary>
    [JsonProperty("thickness")]
    public double Thickness { get; set; }

    /// <summary>
    /// </summary>
    [JsonProperty("baseLevel")]
    public double BaseLevel { get; set; }

    /// <summary>
    /// </summary>
    [JsonProperty("baseOffset")]
    public double BaseOffset { get; set; }

    /// <summary>
    /// </summary>
    public Dictionary<string, double> Parameters { get; set; }
}