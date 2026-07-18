using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitMCPCommandSet.Models.Common
{
    /// <summary>
    /// </summary>
    public class FilterSetting
    {
        /// <summary>
        /// Gets or sets the Revit built-in category name to filter by (e.g. "OST_Walls").
        /// </summary>
        [JsonProperty("filterCategory")]
        public string FilterCategory { get; set; } = null;
        /// <summary>
        /// Gets or sets the Revit element type name to filter by (e.g. "Wall" or "Autodesk.Revit.DB.Wall").
        /// </summary>
        [JsonProperty("filterElementType")]
        public string FilterElementType { get; set; } = null;
        /// <summary>
        /// Gets or sets the ElementId value of the family symbol to filter by.
        /// Zero or negative disables family filtering.
        /// </summary>
        [JsonProperty("filterFamilySymbolId")]
        public int FilterFamilySymbolId { get; set; } = -1;
        /// <summary>
        /// </summary>
        [JsonProperty("includeTypes")]
        public bool IncludeTypes { get; set; } = false;
        /// <summary>
        /// </summary>
        [JsonProperty("includeInstances")]
        public bool IncludeInstances { get; set; } = true;
        /// <summary>
        /// Gets or sets whether to return only elements visible in the current view.
        /// </summary>
        [JsonProperty("filterVisibleInCurrentView")]
        public bool FilterVisibleInCurrentView { get; set; }
        /// <summary>
        /// Gets or sets the minimum point of the bounding box filter (mm).
        /// </summary>
        [JsonProperty("boundingBoxMin")]
        public JZPoint BoundingBoxMin { get; set; } = null;
        /// <summary>
        /// Gets or sets the maximum point of the bounding box filter (mm).
        /// </summary>
        [JsonProperty("boundingBoxMax")]
        public JZPoint BoundingBoxMax { get; set; } = null;
        /// <summary>
        /// </summary>
        [JsonProperty("maxElements")]
        public int MaxElements { get; set; } = 50; 
        /// <summary>
        /// </summary>
        /// <returns>True if the settings are valid, otherwise false</returns>
        public bool Validate(out string errorMessage)
        {
            errorMessage = null;

            // At least one element kind must be selected
            if (!IncludeTypes && !IncludeInstances)
            {
                errorMessage = "Invalid filter settings: must include element types or element instances";
                return false;
            }

            // At least one filter criterion must be specified
            if (string.IsNullOrWhiteSpace(FilterCategory) &&
                string.IsNullOrWhiteSpace(FilterElementType) &&
                FilterFamilySymbolId <= 0)
            {
                errorMessage = "Invalid filter settings: at least one filter criterion (category, element type, or family type) is required";
                return false;
            }

            // Check conflicts between type-only filtering and other filters
            if (IncludeTypes && !IncludeInstances)
            {
                List<string> invalidFilters = new List<string>();
                if (FilterFamilySymbolId > 0)
                    invalidFilters.Add("family instance filter");
                if (FilterVisibleInCurrentView)
                    invalidFilters.Add("view visibility filter");
                if (invalidFilters.Count > 0)
                {
                    errorMessage = $"When filtering types only, the following filters do not apply: {string.Join(", ", invalidFilters)}";
                    return false;
                }
            }
            // Validate the bounding box filter
            if (BoundingBoxMin != null && BoundingBoxMax != null)
            {
                // Minimum point must not exceed the maximum point
                if (BoundingBoxMin.X > BoundingBoxMax.X ||
                    BoundingBoxMin.Y > BoundingBoxMax.Y ||
                    BoundingBoxMin.Z > BoundingBoxMax.Z)
                {
                    errorMessage = "Invalid bounding box filter: minimum point must be less than or equal to maximum point";
                    return false;
                }
            }
            else if (BoundingBoxMin != null || BoundingBoxMax != null)
            {
                errorMessage = "Invalid bounding box filter: both minimum and maximum points must be set";
                return false;
            }
            return true;
        }
    }
}
