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
///     Type of opening
/// </summary>
public enum OpeningType
{
    WallOpening,
    FloorOpening,
    RoofOpening,
    ShaftOpening
}

/// <summary>
///     Shape of opening
/// </summary>
public enum OpeningShape
{
    Rectangular,
    Circular,
    Custom
}

/// <summary>
///     Information about opening creation parameters
/// </summary>
public class OpeningCreationInfo
{
    /// <summary>
    ///     Default constructor
    /// </summary>
    public OpeningCreationInfo()
    {
        BoundaryPoints = new List<JZPoint>();
        Options = new Dictionary<string, object>();
    }

    /// <summary>
    ///     Constructor with basic parameters
    /// </summary>
    /// <param name="openingType">Type of opening</param>
    /// <param name="location">Location point for the opening</param>
    /// <param name="width">Width of the opening (mm)</param>
    /// <param name="height">Height of the opening (mm)</param>
    public OpeningCreationInfo(OpeningType openingType, JZPoint location, double width, double height)
    {
        OpeningType = openingType;
        Location = location;
        Width = width;
        Height = height;
        BoundaryPoints = new List<JZPoint>();
        Options = new Dictionary<string, object>();
    }

    /// <summary>
    ///     Type of opening
    /// </summary>
    [JsonProperty("openingType")]
    public OpeningType OpeningType { get; set; } = OpeningType.WallOpening;

    /// <summary>
    ///     Shape of opening
    /// </summary>
    [JsonProperty("shape")]
    public OpeningShape Shape { get; set; } = OpeningShape.Rectangular;

    /// <summary>
    ///     Location point for the opening
    /// </summary>
    [JsonProperty("location")]
    public JZPoint Location { get; set; }

    /// <summary>
    ///     Direction vector for the opening (for rectangular openings)
    /// </summary>
    [JsonProperty("direction")]
    public JZPoint Direction { get; set; }

    /// <summary>
    ///     Boundary points for custom shapes
    /// </summary>
    [JsonProperty("boundaryPoints")]
    public List<JZPoint> BoundaryPoints { get; set; }

    /// <summary>
    ///     Host element ID
    /// </summary>
    [JsonProperty("hostElementId")]
    public int HostElementId { get; set; }

    /// <summary>
    ///     Width of the opening (mm)
    /// </summary>
    [JsonProperty("width")]
    public double Width { get; set; }

    /// <summary>
    ///     Height of the opening (mm)
    /// </summary>
    [JsonProperty("height")]
    public double Height { get; set; }

    /// <summary>
    ///     Length of the opening (mm) - for rectangular floor/roof openings
    /// </summary>
    [JsonProperty("length")]
    public double Length { get; set; }

    /// <summary>
    ///     Sill height for wall openings (mm)
    /// </summary>
    [JsonProperty("sillHeight")]
    public double SillHeight { get; set; }

    /// <summary>
    ///     Additional options
    /// </summary>
    [JsonProperty("options")]
    public Dictionary<string, object> Options { get; set; }
}