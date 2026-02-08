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

namespace RevitMCPCommandSet.Models.Architecture;

/// <summary>
///     Information about ramp creation parameters
/// </summary>
public class RampCreationInfo
{
    /// <summary>
    ///     Default constructor
    /// </summary>
    public RampCreationInfo()
    {
        PathPoints = new List<JZPoint>();
        BoundaryPoints = new List<JZPoint>();
        Options = new Dictionary<string, object>();
    }

    /// <summary>
    ///     Constructor with basic parameters
    /// </summary>
    /// <param name="baseLevel">Base level elevation (mm)</param>
    /// <param name="topLevel">Top level elevation (mm)</param>
    /// <param name="width">Ramp width (mm)</param>
    /// <param name="pathPoints">Path points for the ramp</param>
    public RampCreationInfo(double baseLevel, double topLevel, double width, List<JZPoint> pathPoints)
    {
        BaseLevel = baseLevel;
        TopLevel = topLevel;
        Width = width;
        PathPoints = pathPoints ?? new List<JZPoint>();
        BoundaryPoints = new List<JZPoint>();
        Options = new Dictionary<string, object>();
    }

    /// <summary>
    ///     Start point of the ramp
    /// </summary>
    [JsonProperty("startPoint")]
    public JZPoint StartPoint { get; set; }

    /// <summary>
    ///     End point of the ramp
    /// </summary>
    [JsonProperty("endPoint")]
    public JZPoint EndPoint { get; set; }

    /// <summary>
    ///     Path points defining the ramp path
    /// </summary>
    [JsonProperty("pathPoints")]
    public List<JZPoint> PathPoints { get; set; }

    /// <summary>
    ///     Boundary points defining the ramp outline (for custom ramps)
    /// </summary>
    [JsonProperty("boundaryPoints")]
    public List<JZPoint> BoundaryPoints { get; set; }

    /// <summary>
    ///     Base level elevation (mm)
    /// </summary>
    [JsonProperty("baseLevel")]
    public double BaseLevel { get; set; }

    /// <summary>
    ///     Top level elevation (mm)
    /// </summary>
    [JsonProperty("topLevel")]
    public double TopLevel { get; set; }

    /// <summary>
    ///     Base offset (mm)
    /// </summary>
    [JsonProperty("baseOffset")]
    public double BaseOffset { get; set; }

    /// <summary>
    ///     Top offset (mm)
    /// </summary>
    [JsonProperty("topOffset")]
    public double TopOffset { get; set; }

    /// <summary>
    ///     Ramp width (mm)
    /// </summary>
    [JsonProperty("width")]
    public double Width { get; set; }

    /// <summary>
    ///     Ramp slope (percent)
    /// </summary>
    [JsonProperty("slope")]
    public double Slope { get; set; }

    /// <summary>
    ///     Ramp type ID in Revit
    /// </summary>
    [JsonProperty("typeId")]
    public int TypeId { get; set; }

    /// <summary>
    ///     Ramp type name
    /// </summary>
    [JsonProperty("rampType")]
    public string RampType { get; set; }

    /// <summary>
    ///     Ramp material
    /// </summary>
    [JsonProperty("material")]
    public string Material { get; set; }

    /// <summary>
    ///     Is this a custom ramp (using form instead of built-in ramp)
    /// </summary>
    [JsonProperty("isCustomRamp")]
    public bool IsCustomRamp { get; set; }

    /// <summary>
    ///     Additional options
    /// </summary>
    [JsonProperty("options")]
    public Dictionary<string, object> Options { get; set; }
}