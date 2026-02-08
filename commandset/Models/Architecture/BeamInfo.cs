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
///     Information about a structural beam
/// </summary>
public class BeamInfo
{
    /// <summary>
    ///     Default constructor
    /// </summary>
    public BeamInfo()
    {
        StartPoint = new JZPoint(0, 0, 0);
        EndPoint = new JZPoint(0, 0, 0);
        Options = new Dictionary<string, object>();
    }

    /// <summary>
    ///     Constructor with position and dimensions
    /// </summary>
    /// <param name="startX">Start X position (mm)</param>
    /// <param name="startY">Start Y position (mm)</param>
    /// <param name="startZ">Start Z position (mm)</param>
    /// <param name="endX">End X position (mm)</param>
    /// <param name="endY">End Y position (mm)</param>
    /// <param name="endZ">End Z position (mm)</param>
    /// <param name="width">Width (mm)</param>
    /// <param name="height">Height (mm)</param>
    /// <param name="level">Level elevation (mm)</param>
    public BeamInfo(double startX, double startY, double startZ, double endX, double endY, double endZ,
        double width, double height, double level = 0)
    {
        StartPoint = new JZPoint(startX, startY, startZ);
        EndPoint = new JZPoint(endX, endY, endZ);
        Width = width;
        Height = height;
        Level = level;
    }

    /// <summary>
    ///     Beam start position (mm)
    /// </summary>
    [JsonProperty("startPoint")]
    public JZPoint StartPoint { get; set; }

    /// <summary>
    ///     Beam end position (mm)
    /// </summary>
    [JsonProperty("endPoint")]
    public JZPoint EndPoint { get; set; }

    /// <summary>
    ///     Beam width (mm)
    /// </summary>
    [JsonProperty("width")]
    public double Width { get; set; } = 300;

    /// <summary>
    ///     Beam height (mm)
    /// </summary>
    [JsonProperty("height")]
    public double Height { get; set; } = 500;

    /// <summary>
    ///     Host level elevation (mm)
    /// </summary>
    [JsonProperty("level")]
    public double Level { get; set; }

    /// <summary>
    ///     Beam offset from level (mm)
    /// </summary>
    [JsonProperty("levelOffset")]
    public double LevelOffset { get; set; }

    /// <summary>
    ///     Beam type
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = "Concrete"; // Concrete, Steel, Wood, etc.

    /// <summary>
    ///     Beam family type ID in Revit
    /// </summary>
    [JsonProperty("typeId")]
    public int TypeId { get; set; } = -1;

    /// <summary>
    ///     Beam material
    /// </summary>
    [JsonProperty("material")]
    public string Material { get; set; } = "Concrete";

    /// <summary>
    ///     Structural parameters: Is load-bearing
    /// </summary>
    [JsonProperty("isLoadBearing")]
    public bool IsLoadBearing { get; set; } = true;

    /// <summary>
    ///     Additional options
    /// </summary>
    [JsonProperty("options")]
    public Dictionary<string, object> Options { get; set; }
}