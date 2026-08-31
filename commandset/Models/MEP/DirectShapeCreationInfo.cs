// 
//                       RevitAPI-Solutions
// Copyright (c) Duong Tran Quang (DTDucas) (baymax.contact@gmail.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//

using Newtonsoft.Json;
using RevitMCPCommandSet.Models.Common;

namespace RevitMCPCommandSet.Models.MEP;

/// <summary>
///     Information for direct shape creation parameters
/// </summary>
public class DirectShapeCreationInfo
{
    /// <summary>
    ///     Default constructor
    /// </summary>
    public DirectShapeCreationInfo()
    {
        Center = new JZPoint(0, 0, 0);
        Points = new List<JZPoint>();
        ExtrusionDir = new JZPoint(0, 0, 1);
        Options = new Dictionary<string, object>();
    }

    /// <summary>
    ///     Shape type (Box/Cylinder/Extrusion)
    /// </summary>
    [JsonProperty("shapeType")]
    public string ShapeType { get; set; } = "Box";

    /// <summary>
    ///     Width in mm (Box)
    /// </summary>
    [JsonProperty("width")]
    public double Width { get; set; }

    /// <summary>
    ///     Depth in mm (Box)
    /// </summary>
    [JsonProperty("depth")]
    public double Depth { get; set; }

    /// <summary>
    ///     Height in mm (Box/Cylinder/Extrusion)
    /// </summary>
    [JsonProperty("height")]
    public double Height { get; set; }

    /// <summary>
    ///     Radius in mm (Cylinder)
    /// </summary>
    [JsonProperty("radius")]
    public double Radius { get; set; }

    /// <summary>
    ///     Center point position (mm)
    /// </summary>
    [JsonProperty("center")]
    public JZPoint Center { get; set; }

    /// <summary>
    ///     Curve type for extrusion profile (Line/Arc)
    /// </summary>
    [JsonProperty("curveType")]
    public string CurveType { get; set; } = "Line";

    /// <summary>
    ///     Profile points for extrusion (mm)
    /// </summary>
    [JsonProperty("points")]
    public List<JZPoint> Points { get; set; }

    /// <summary>
    ///     Extrusion direction vector
    /// </summary>
    [JsonProperty("extrusionDir")]
    public JZPoint ExtrusionDir { get; set; }

    /// <summary>
    ///     Extrusion length in mm
    /// </summary>
    [JsonProperty("extrusionLength")]
    public double ExtrusionLength { get; set; }

    /// <summary>
    ///     Target category name
    /// </summary>
    [JsonProperty("category")]
    public string Category { get; set; }

    /// <summary>
    ///     Material name
    /// </summary>
    [JsonProperty("material")]
    public string Material { get; set; }

    /// <summary>
    ///     Type ID in Revit
    /// </summary>
    [JsonProperty("typeId")]
    public int TypeId { get; set; }

    /// <summary>
    ///     Additional options
    /// </summary>
    [JsonProperty("options")]
    public Dictionary<string, object> Options { get; set; }
}
