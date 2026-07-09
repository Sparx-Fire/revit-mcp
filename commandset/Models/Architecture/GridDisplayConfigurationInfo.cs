using Newtonsoft.Json;

namespace RevitMCPCommandSet.Models.Architecture;

/// <summary>
///     Parameters for configuring grid 2D extents and bubble display on floor plans.
/// </summary>
public class GridDisplayConfigurationInfo
{
    /// <summary>
    ///     Grid element IDs to update. Empty means all grids in the project.
    /// </summary>
    [JsonProperty("gridIds")]
    public List<long> GridIds { get; set; } = new();

    /// <summary>
    ///     GridType name from the project.
    /// </summary>
    [JsonProperty("gridTypeName")]
    public string GridTypeName { get; set; } = string.Empty;

    /// <summary>
    ///     GridType element ID. Used when name lookup fails.
    /// </summary>
    [JsonProperty("gridTypeId")]
    public int GridTypeId { get; set; } = -1;

    /// <summary>
    ///     Minimum X extent in millimeters for horizontal grids.
    /// </summary>
    [JsonProperty("xExtentMin")]
    public double? XExtentMin { get; set; }

    /// <summary>
    ///     Maximum X extent in millimeters for horizontal grids.
    /// </summary>
    [JsonProperty("xExtentMax")]
    public double? XExtentMax { get; set; }

    /// <summary>
    ///     Minimum Y extent in millimeters for vertical grids.
    /// </summary>
    [JsonProperty("yExtentMin")]
    public double? YExtentMin { get; set; }

    /// <summary>
    ///     Maximum Y extent in millimeters for vertical grids.
    /// </summary>
    [JsonProperty("yExtentMax")]
    public double? YExtentMax { get; set; }

    /// <summary>
    ///     Show bubbles at both grid ends on floor plans.
    /// </summary>
    [JsonProperty("showBubbles")]
    public bool ShowBubbles { get; set; } = true;

    /// <summary>
    ///     Apply 2D extents to all non-template floor plans.
    /// </summary>
    [JsonProperty("applyToAllFloorPlans")]
    public bool ApplyToAllFloorPlans { get; set; } = true;
}

/// <summary>
///     Result of grid display configuration.
/// </summary>
public class GridDisplayConfigurationResult
{
    [JsonProperty("gridsProcessed")]
    public int GridsProcessed { get; set; }

    [JsonProperty("viewsProcessed")]
    public int ViewsProcessed { get; set; }

    [JsonProperty("gridViewUpdates")]
    public int GridViewUpdates { get; set; }

    [JsonProperty("gridTypeName")]
    public string GridTypeName { get; set; } = string.Empty;

    [JsonProperty("warnings")]
    public List<string> Warnings { get; set; } = new();
}
