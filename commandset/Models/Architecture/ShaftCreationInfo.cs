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
///     Shape of shaft
/// </summary>
public enum ShaftShape
{
    Rectangular,
    Circular,
    Custom
}

/// <summary>
///     Information about shaft creation parameters
/// </summary>
public class ShaftCreationInfo
{
    /// <summary>
    ///     Default constructor
    /// </summary>
    public ShaftCreationInfo()
    {
        BoundaryPoints = new List<JZPoint>();
        Options = new Dictionary<string, object>();
    }

    /// <summary>
    ///     Constructor with basic parameters
    /// </summary>
    /// <param name="location">Location point for the shaft</param>
    /// <param name="width">Width of the shaft (mm)</param>
    /// <param name="length">Length of the shaft (mm)</param>
    /// <param name="baseLevel">Base level elevation (mm)</param>
    /// <param name="topLevel">Top level elevation (mm)</param>
    public ShaftCreationInfo(JZPoint location, double width, double length, double baseLevel, double topLevel)
    {
        Location = location;
        Width = width;
        Length = length;
        BaseLevel = baseLevel;
        TopLevel = topLevel;
        BoundaryPoints = new List<JZPoint>();
        Options = new Dictionary<string, object>();
    }

    /// <summary>
    ///     Shape of shaft
    /// </summary>
    [JsonProperty("shape")]
    public ShaftShape Shape { get; set; } = ShaftShape.Rectangular;

    /// <summary>
    ///     Location point for the shaft
    /// </summary>
    [JsonProperty("location")]
    public JZPoint Location { get; set; }

    /// <summary>
    ///     Direction vector for the shaft (for rectangular shafts)
    /// </summary>
    [JsonProperty("direction")]
    public JZPoint Direction { get; set; }

    /// <summary>
    ///     Boundary points for custom shapes
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
    ///     Width of the shaft (mm)
    /// </summary>
    [JsonProperty("width")]
    public double Width { get; set; }

    /// <summary>
    ///     Length of the shaft (mm) - for rectangular shafts
    /// </summary>
    [JsonProperty("length")]
    public double Length { get; set; }

    /// <summary>
    ///     Shaft name
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; set; } = "Shaft";

    /// <summary>
    ///     Shaft type (Elevator, Mechanical, etc.)
    /// </summary>
    [JsonProperty("shaftType")]
    public string ShaftType { get; set; } = "Elevator";

    /// <summary>
    ///     Additional options
    /// </summary>
    [JsonProperty("options")]
    public Dictionary<string, object> Options { get; set; }
}