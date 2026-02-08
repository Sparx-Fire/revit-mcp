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
///     Information about wall creation parameters
/// </summary>
public class WallCreationInfo
{
    /// <summary>
    ///     Default constructor
    /// </summary>
    public WallCreationInfo()
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
    /// <param name="height">Wall height (mm)</param>
    /// <param name="thickness">Wall thickness (mm)</param>
    /// <param name="level">Base level elevation (mm)</param>
    public WallCreationInfo(double startX, double startY, double startZ, double endX, double endY, double endZ,
        double height, double thickness, double level = 0)
    {
        StartPoint = new JZPoint(startX, startY, startZ);
        EndPoint = new JZPoint(endX, endY, endZ);
        Height = height;
        Thickness = thickness;
        BaseLevel = level;
    }

    /// <summary>
    ///     Wall start position (mm)
    /// </summary>
    [JsonProperty("startPoint")]
    public JZPoint StartPoint { get; set; }

    /// <summary>
    ///     Wall end position (mm)
    /// </summary>
    [JsonProperty("endPoint")]
    public JZPoint EndPoint { get; set; }

    /// <summary>
    ///     Wall height (mm)
    /// </summary>
    [JsonProperty("height")]
    public double Height { get; set; }

    /// <summary>
    ///     Wall thickness (mm)
    /// </summary>
    [JsonProperty("thickness")]
    public double Thickness { get; set; }

    /// <summary>
    ///     Base level elevation (mm)
    /// </summary>
    [JsonProperty("baseLevel")]
    public double BaseLevel { get; set; }

    /// <summary>
    ///     Base offset (mm)
    /// </summary>
    [JsonProperty("baseOffset")]
    public double BaseOffset { get; set; }

    /// <summary>
    ///     Top constraint type (0=Unconstrained, 1=Up to level, 2=Unconnected height)
    /// </summary>
    [JsonProperty("topConstraintType")]
    public int TopConstraintType { get; set; }

    /// <summary>
    ///     Top level ID (only used if TopConstraintType = 1)
    /// </summary>
    [JsonProperty("topLevelId")]
    public int TopLevelId { get; set; }

    /// <summary>
    ///     Top offset (mm) (used if TopConstraintType = 1)
    /// </summary>
    [JsonProperty("topOffset")]
    public double TopOffset { get; set; }

    /// <summary>
    ///     Wall type
    /// </summary>
    [JsonProperty("wallType")]
    public string WallType { get; set; } = "Basic Wall"; // Basic Wall, Curtain Wall, etc.

    /// <summary>
    ///     Wall type ID in Revit
    /// </summary>
    [JsonProperty("typeId")]
    public int TypeId { get; set; }

    /// <summary>
    ///     Wall material
    /// </summary>
    [JsonProperty("material")]
    public string Material { get; set; } = "Concrete";

    /// <summary>
    ///     Is structural
    /// </summary>
    [JsonProperty("isStructural")]
    public bool IsStructural { get; set; } = true;

    /// <summary>
    ///     Flip wall direction
    /// </summary>
    [JsonProperty("flipped")]
    public bool Flipped { get; set; }

    /// <summary>
    ///     Additional options
    /// </summary>
    [JsonProperty("options")]
    public Dictionary<string, object> Options { get; set; }
}