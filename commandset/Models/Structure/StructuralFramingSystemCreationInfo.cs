using Newtonsoft.Json;

namespace RevitMCPCommandSet.Models.Structure;

/// <summary>
/// Model for creating structural beam framing systems
/// </summary>
public class StructuralFramingSystemCreationInfo
{
    /// <summary>
    /// Name of the level to place the beam system on
    /// </summary>
    [JsonProperty("levelName")]
    public string LevelName { get; set; }

    /// <summary>
    /// Minimum X coordinate of rectangular boundary in millimeters
    /// </summary>
    [JsonProperty("xMin")]
    public double XMin { get; set; }

    /// <summary>
    /// Maximum X coordinate of rectangular boundary in millimeters
    /// </summary>
    [JsonProperty("xMax")]
    public double XMax { get; set; }

    /// <summary>
    /// Minimum Y coordinate of rectangular boundary in millimeters
    /// </summary>
    [JsonProperty("yMin")]
    public double YMin { get; set; }

    /// <summary>
    /// Maximum Y coordinate of rectangular boundary in millimeters
    /// </summary>
    [JsonProperty("yMax")]
    public double YMax { get; set; }

    /// <summary>
    /// Which edge defines the beam direction: "bottom", "right", "top", or "left"
    /// </summary>
    [JsonProperty("directionEdge")]
    public string DirectionEdge { get; set; } = "bottom";

    /// <summary>
    /// Layout rule type (v1: only "fixed_distance" supported)
    /// </summary>
    [JsonProperty("layoutRule")]
    public string LayoutRule { get; set; } = "fixed_distance";

    /// <summary>
    /// Spacing between beams in millimeters
    /// </summary>
    [JsonProperty("spacing")]
    public double Spacing { get; set; }

    /// <summary>
    /// Beam justification: "beginning", "center", "end", or "directionline"
    /// </summary>
    [JsonProperty("justify")]
    public string Justify { get; set; } = "center";

    /// <summary>
    /// Beam family type name (optional, auto-selects if not provided)
    /// </summary>
    [JsonProperty("beamTypeName")]
    public string BeamTypeName { get; set; }

    /// <summary>
    /// Elevation offset from level in millimeters
    /// </summary>
    [JsonProperty("elevation")]
    public double Elevation { get; set; } = 0;

    /// <summary>
    /// Whether to create a 3D beam system
    /// </summary>
    [JsonProperty("is3d")]
    public bool Is3D { get; set; } = false;

    /// <summary>
    /// Validates the beam system creation parameters
    /// </summary>
    /// <param name="errorMessage">Error message if validation fails</param>
    /// <returns>True if valid, false otherwise</returns>
    public bool Validate(out string errorMessage)
    {
        // Check boundary validity
        if (XMin >= XMax)
        {
            errorMessage = "xMin must be less than xMax";
            return false;
        }

        if (YMin >= YMax)
        {
            errorMessage = "yMin must be less than yMax";
            return false;
        }

        // Check spacing
        if (Spacing <= 0)
        {
            errorMessage = "spacing must be greater than 0";
            return false;
        }

        // Validate directionEdge
        string[] validDirections = { "bottom", "right", "top", "left" };
        if (!validDirections.Contains(DirectionEdge?.ToLower()))
        {
            errorMessage = $"directionEdge must be one of: {string.Join(", ", validDirections)}";
            return false;
        }

        // Validate layoutRule (v1: only fixed_distance)
        if (!string.Equals(LayoutRule, "fixed_distance", StringComparison.OrdinalIgnoreCase))
        {
            errorMessage = "layoutRule must be 'fixed_distance' (only supported option in v1)";
            return false;
        }

        // Validate justify
        string[] validJustify = { "beginning", "center", "end", "directionline" };
        if (!validJustify.Contains(Justify?.ToLower()))
        {
            errorMessage = $"justify must be one of: {string.Join(", ", validJustify)}";
            return false;
        }

        // Check level name
        if (string.IsNullOrWhiteSpace(LevelName))
        {
            errorMessage = "levelName cannot be empty";
            return false;
        }

        // Validate spacing vs. boundary (warning if spacing > dimension)
        double width = XMax - XMin;
        double height = YMax - YMin;

        if (DirectionEdge.ToLower() == "bottom" || DirectionEdge.ToLower() == "top")
        {
            // Beams run perpendicular to direction edge (along Y)
            // Spacing applies in X direction
            if (Spacing > width)
            {
                errorMessage = $"Warning: spacing ({Spacing}mm) is greater than boundary width ({width}mm). This may create very few beams.";
                // This is a warning, not a hard fail
            }
        }
        else // left or right
        {
            // Beams run perpendicular to direction edge (along X)
            // Spacing applies in Y direction
            if (Spacing > height)
            {
                errorMessage = $"Warning: spacing ({Spacing}mm) is greater than boundary height ({height}mm). This may create very few beams.";
                // This is a warning, not a hard fail
            }
        }

        errorMessage = string.Empty;
        return true;
    }
}

/// <summary>
/// Result model for beam system creation
/// </summary>
public class StructuralFramingSystemCreationResult
{
    [JsonProperty("beamSystemId")]
    public long BeamSystemId { get; set; }

    [JsonProperty("beamIds")]
    public List<long> BeamIds { get; set; }

    [JsonProperty("beamCount")]
    public int BeamCount { get; set; }

    [JsonProperty("actualSpacing")]
    public double ActualSpacing { get; set; }  // mm

    [JsonProperty("usedBeamTypeName")]
    public string UsedBeamTypeName { get; set; }

    [JsonProperty("levelName")]
    public string LevelName { get; set; }
}
