using Newtonsoft.Json;

namespace RevitMCPCommandSet.Models.Annotation;

/// <summary>
///     Parameters for creating chained room wall dimensions on a floor plan.
/// </summary>
public class RoomWallDimensionInfo
{
    /// <summary>
    ///     Room element ID.
    /// </summary>
    [JsonProperty("roomId")]
    public int RoomId { get; set; }

    /// <summary>
    ///     Offset of the dimension line from room boundary in millimeters.
    /// </summary>
    [JsonProperty("offsetMm")]
    public double OffsetMm { get; set; } = 300;

    /// <summary>
    ///     Dimension type name from the project (e.g. "Linear - 3mm Arial").
    /// </summary>
    [JsonProperty("dimensionType")]
    public string DimensionType { get; set; } = string.Empty;

    /// <summary>
    ///     Dimension style element ID. Used when name lookup fails.
    /// </summary>
    [JsonProperty("dimensionStyleId")]
    public int DimensionStyleId { get; set; } = -1;

    /// <summary>
    ///     View ID. -1 uses the active view.
    /// </summary>
    [JsonProperty("viewId")]
    public int ViewId { get; set; } = -1;
}
