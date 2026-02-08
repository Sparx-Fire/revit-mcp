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
///     Information about stair creation parameters
/// </summary>
public class StairCreationInfo
{
    /// <summary>
    ///     Default constructor
    /// </summary>
    public StairCreationInfo()
    {
        PathPoints = new List<JZPoint>();
        Options = new Dictionary<string, object>();
    }

    /// <summary>
    ///     Constructor with basic parameters
    /// </summary>
    /// <param name="baseLevel">Base level elevation (mm)</param>
    /// <param name="topLevel">Top level elevation (mm)</param>
    /// <param name="width">Stair width (mm)</param>
    /// <param name="riserHeight">Riser height (mm)</param>
    /// <param name="treadDepth">Tread depth (mm)</param>
    public StairCreationInfo(double baseLevel, double topLevel, double width, double riserHeight, double treadDepth)
    {
        BaseLevel = baseLevel;
        TopLevel = topLevel;
        Width = width;
        RiserHeight = riserHeight;
        TreadDepth = treadDepth;
        PathPoints = new List<JZPoint>();
        Options = new Dictionary<string, object>();
    }

    /// <summary>
    ///     Location point of the stair (starting point)
    /// </summary>
    [JsonProperty("location")]
    public JZPoint Location { get; set; }

    /// <summary>
    ///     Direction vector for the stair (XY plane)
    /// </summary>
    [JsonProperty("direction")]
    public JZPoint Direction { get; set; }

    /// <summary>
    ///     Start point of the stair
    /// </summary>
    [JsonProperty("startPoint")]
    public JZPoint StartPoint { get; set; }

    /// <summary>
    ///     End point of the stair
    /// </summary>
    [JsonProperty("endPoint")]
    public JZPoint EndPoint { get; set; }

    /// <summary>
    ///     Path points defining the stair path
    /// </summary>
    [JsonProperty("pathPoints")]
    public List<JZPoint> PathPoints { get; set; }

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
    ///     Stair width (mm)
    /// </summary>
    [JsonProperty("width")]
    public double Width { get; set; }

    /// <summary>
    ///     Riser height (mm)
    /// </summary>
    [JsonProperty("riserHeight")]
    public double RiserHeight { get; set; }

    /// <summary>
    ///     Tread depth (mm)
    /// </summary>
    [JsonProperty("treadDepth")]
    public double TreadDepth { get; set; }

    /// <summary>
    ///     Number of steps (if specified)
    /// </summary>
    [JsonProperty("stepCount")]
    public int StepCount { get; set; }

    /// <summary>
    ///     Stair type ID in Revit
    /// </summary>
    [JsonProperty("typeId")]
    public int TypeId { get; set; }

    /// <summary>
    ///     Stair type name
    /// </summary>
    [JsonProperty("stairType")]
    public string StairType { get; set; } = "Standard";

    /// <summary>
    ///     Stair material
    /// </summary>
    [JsonProperty("material")]
    public string Material { get; set; } = "Concrete";

    /// <summary>
    ///     Does the stair have a landing
    /// </summary>
    [JsonProperty("hasLanding")]
    public bool HasLanding { get; set; }

    /// <summary>
    ///     Landing width (if hasLanding is true)
    /// </summary>
    [JsonProperty("landingWidth")]
    public double LandingWidth { get; set; }

    /// <summary>
    ///     Landing depth (if hasLanding is true)
    /// </summary>
    [JsonProperty("landingDepth")]
    public double LandingDepth { get; set; }

    /// <summary>
    ///     Additional options
    /// </summary>
    [JsonProperty("options")]
    public Dictionary<string, object> Options { get; set; }
}