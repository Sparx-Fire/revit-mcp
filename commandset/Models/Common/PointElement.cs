using Newtonsoft.Json;

namespace RevitMCPCommandSet.Models.Common;

/// <summary>
/// </summary>
public class PointElement
{
    public PointElement()
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
    [JsonProperty("locationPoint")]
    public JZPoint LocationPoint { get; set; }

    /// <summary>
    /// </summary>
    [JsonProperty("width")]
    public double Width { get; set; } = -1;

    /// <summary>
    /// </summary>
    [JsonProperty("depth")]
    public double Depth { get; set; }

    /// <summary>
    /// </summary>
    [JsonProperty("height")]
    public double Height { get; set; }

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
    [JsonProperty("rotation")]
    public double Rotation { get; set; } = 0;

    /// <summary>
    /// </summary>
    [JsonProperty("hostWallId")]
    public int HostWallId { get; set; } = -1;

    /// <summary>
    /// </summary>
    [JsonProperty("facingFlipped")]
    public bool FacingFlipped { get; set; } = false;

    /// <summary>
    /// </summary>
    [JsonProperty("parameters")]
    public Dictionary<string, double> Parameters { get; set; }
}
