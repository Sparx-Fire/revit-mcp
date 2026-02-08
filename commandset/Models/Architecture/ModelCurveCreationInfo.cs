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

namespace RevitMCPCommandSet.Models.Architecture;

/// <summary>
///     Information for model curve creation
/// </summary>
public class ModelCurveCreationInfo
{
    /// <summary>
    ///     Default constructor
    /// </summary>
    public ModelCurveCreationInfo()
    {
        Points = new List<Point>();
        Parameters = new Dictionary<string, object>();
    }

    /// <summary>
    ///     Curve type (Line, Arc, Circle, Ellipse, Spline)
    /// </summary>
    [JsonProperty("curveType")]
    public string CurveType { get; set; } = "Line";

    /// <summary>
    ///     Points defining the curve geometry
    /// </summary>
    [JsonProperty("points")]
    public List<Point> Points { get; set; }

    /// <summary>
    ///     Center point for circle, arc, ellipse
    /// </summary>
    [JsonProperty("center")]
    public Point Center { get; set; }

    /// <summary>
    ///     Radius for circle or arc in millimeters
    /// </summary>
    [JsonProperty("radius")]
    public double? Radius { get; set; }

    /// <summary>
    ///     X radius for ellipse in millimeters
    /// </summary>
    [JsonProperty("radiusX")]
    public double? RadiusX { get; set; }

    /// <summary>
    ///     Y radius for ellipse in millimeters
    /// </summary>
    [JsonProperty("radiusY")]
    public double? RadiusY { get; set; }

    /// <summary>
    ///     Rotation angle for ellipse in degrees
    /// </summary>
    [JsonProperty("rotation")]
    public double? Rotation { get; set; }

    /// <summary>
    ///     Start angle for arc in degrees
    /// </summary>
    [JsonProperty("startAngle")]
    public double? StartAngle { get; set; }

    /// <summary>
    ///     End angle for arc in degrees
    /// </summary>
    [JsonProperty("endAngle")]
    public double? EndAngle { get; set; }

    /// <summary>
    ///     Normal vector for curve plane
    /// </summary>
    [JsonProperty("normal")]
    public Point Normal { get; set; }

    /// <summary>
    ///     Sketch plane ID for curve
    /// </summary>
    [JsonProperty("sketchPlaneId")]
    public int SketchPlaneId { get; set; }

    /// <summary>
    ///     Line style name
    /// </summary>
    [JsonProperty("lineStyle")]
    public string LineStyle { get; set; }

    /// <summary>
    ///     Additional parameters
    /// </summary>
    [JsonProperty("parameters")]
    public Dictionary<string, object> Parameters { get; set; }
}