using Newtonsoft.Json;

namespace RevitMCPCommandSet.Models.Architecture;

public class GridInfo
{
    /// <summary>
    ///     X-axis grid line positions in millimeters
    /// </summary>
    [JsonProperty("xAxis")]
    public List<double> XAxis { get; set; } = new();

    /// <summary>
    ///     Y-axis grid line positions in millimeters
    /// </summary>
    [JsonProperty("yAxis")]
    public List<double> YAxis { get; set; } = new();

    /// <summary>
    ///     Optional labels for X-axis grid lines
    /// </summary>
    [JsonProperty("xLabels")]
    public List<string> XLabels { get; set; } = new();

    /// <summary>
    ///     Optional labels for Y-axis grid lines
    /// </summary>
    [JsonProperty("yLabels")]
    public List<string> YLabels { get; set; } = new();

    /// <summary>
    ///     Optional Z-coordinate for the grid system in millimeters
    /// </summary>
    [JsonProperty("elevation")]
    public double Elevation { get; set; }

    /// <summary>
    ///     Optional parameter to indicate if the grid system should extend to 3D
    /// </summary>
    [JsonProperty("extendTo3D")]
    public bool ExtendTo3D { get; set; }

    /// <summary>
    ///     Optional parameter to specify the height of 3D grid lines in millimeters
    /// </summary>
    [JsonProperty("extendHeight")]
    public double ExtendHeight { get; set; }

    /// <summary>
    ///     Optional parameter to specify the bubble visibility
    /// </summary>
    [JsonProperty("bubbleVisible")]
    public bool BubbleVisible { get; set; } = true;

    /// <summary>
    ///     Optional parameter to specify the grid line weight
    /// </summary>
    [JsonProperty("lineWeight")]
    public int LineWeight { get; set; }
}