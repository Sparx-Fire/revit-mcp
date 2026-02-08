using Newtonsoft.Json;

namespace RevitMCPCommandSet.Models.Architecture;

/// <summary>
/// Model for creating grid system with smart spacing generation
/// </summary>
public class GridCreationInfo
{
    /// <summary>
    /// Number of grid lines along X-axis (vertical grids)
    /// </summary>
    [JsonProperty("xCount")]
    public int XCount { get; set; }

    /// <summary>
    /// Spacing between X-axis grid lines in millimeters
    /// </summary>
    [JsonProperty("xSpacing")]
    public double XSpacing { get; set; }

    /// <summary>
    /// Starting label for X-axis grids (e.g., "A" or "1")
    /// </summary>
    [JsonProperty("xStartLabel")]
    public string XStartLabel { get; set; } = "A";

    /// <summary>
    /// Naming style for X-axis: "alphabetic" (A,B,C...) or "numeric" (1,2,3...)
    /// </summary>
    [JsonProperty("xNamingStyle")]
    public string XNamingStyle { get; set; } = "alphabetic";

    /// <summary>
    /// Number of grid lines along Y-axis (horizontal grids)
    /// </summary>
    [JsonProperty("yCount")]
    public int YCount { get; set; }

    /// <summary>
    /// Spacing between Y-axis grid lines in millimeters
    /// </summary>
    [JsonProperty("ySpacing")]
    public double YSpacing { get; set; }

    /// <summary>
    /// Starting label for Y-axis grids (e.g., "1" or "A")
    /// </summary>
    [JsonProperty("yStartLabel")]
    public string YStartLabel { get; set; } = "1";

    /// <summary>
    /// Naming style for Y-axis: "alphabetic" (A,B,C...) or "numeric" (1,2,3...)
    /// </summary>
    [JsonProperty("yNamingStyle")]
    public string YNamingStyle { get; set; } = "numeric";

    /// <summary>
    /// Minimum extent along X-axis in millimeters (from project base point)
    /// </summary>
    [JsonProperty("xExtentMin")]
    public double XExtentMin { get; set; } = 0;

    /// <summary>
    /// Maximum extent along X-axis in millimeters (from project base point)
    /// </summary>
    [JsonProperty("xExtentMax")]
    public double XExtentMax { get; set; } = 50000;

    /// <summary>
    /// Minimum extent along Y-axis in millimeters (from project base point)
    /// </summary>
    [JsonProperty("yExtentMin")]
    public double YExtentMin { get; set; } = 0;

    /// <summary>
    /// Maximum extent along Y-axis in millimeters (from project base point)
    /// </summary>
    [JsonProperty("yExtentMax")]
    public double YExtentMax { get; set; } = 50000;

    /// <summary>
    /// Elevation for grid lines in millimeters (Z-coordinate)
    /// </summary>
    [JsonProperty("elevation")]
    public double Elevation { get; set; } = 0;

    /// <summary>
    /// Starting position for first X-axis grid in millimeters
    /// </summary>
    [JsonProperty("xStartPosition")]
    public double XStartPosition { get; set; } = 0;

    /// <summary>
    /// Starting position for first Y-axis grid in millimeters
    /// </summary>
    [JsonProperty("yStartPosition")]
    public double YStartPosition { get; set; } = 0;

    /// <summary>
    /// Validates the grid creation parameters
    /// </summary>
    /// <param name="errorMessage">Error message if validation fails</param>
    /// <returns>True if valid, false otherwise</returns>
    public bool Validate(out string errorMessage)
    {
        // Check X-axis count
        if (XCount <= 0)
        {
            errorMessage = "xCount must be greater than 0";
            return false;
        }

        // Check Y-axis count
        if (YCount <= 0)
        {
            errorMessage = "yCount must be greater than 0";
            return false;
        }

        // Check X-axis spacing
        if (XSpacing <= 0)
        {
            errorMessage = "xSpacing must be greater than 0";
            return false;
        }

        // Check Y-axis spacing
        if (YSpacing <= 0)
        {
            errorMessage = "ySpacing must be greater than 0";
            return false;
        }

        // Check naming styles
        if (XNamingStyle != "alphabetic" && XNamingStyle != "numeric")
        {
            errorMessage = "xNamingStyle must be 'alphabetic' or 'numeric'";
            return false;
        }

        if (YNamingStyle != "alphabetic" && YNamingStyle != "numeric")
        {
            errorMessage = "yNamingStyle must be 'alphabetic' or 'numeric'";
            return false;
        }

        // Check start labels
        if (string.IsNullOrWhiteSpace(XStartLabel))
        {
            errorMessage = "xStartLabel cannot be empty";
            return false;
        }

        if (string.IsNullOrWhiteSpace(YStartLabel))
        {
            errorMessage = "yStartLabel cannot be empty";
            return false;
        }

        // Check extents are valid
        if (XExtentMin >= XExtentMax)
        {
            errorMessage = "xExtentMin must be less than xExtentMax";
            return false;
        }

        if (YExtentMin >= YExtentMax)
        {
            errorMessage = "yExtentMin must be less than yExtentMax";
            return false;
        }

        // Check that grids will have non-zero length
        double xLineLength = XExtentMax - XExtentMin;
        double yLineLength = YExtentMax - YExtentMin;

        if (xLineLength <= 0)
        {
            errorMessage = "X-axis grid lines must have positive length";
            return false;
        }

        if (yLineLength <= 0)
        {
            errorMessage = "Y-axis grid lines must have positive length";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }
}
