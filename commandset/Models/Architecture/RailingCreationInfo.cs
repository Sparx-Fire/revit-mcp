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
///     Information about railing creation parameters
/// </summary>
public class RailingCreationInfo
{
    /// <summary>
    ///     Default constructor
    /// </summary>
    public RailingCreationInfo()
    {
        PathPoints = new List<JZPoint>();
        Options = new Dictionary<string, object>();
    }

    /// <summary>
    ///     Constructor with basic parameters
    /// </summary>
    /// <param name="level">Base level elevation (mm)</param>
    /// <param name="height">Railing height (mm)</param>
    /// <param name="pathPoints">Path points for the railing</param>
    public RailingCreationInfo(double level, double height, List<JZPoint> pathPoints)
    {
        Level = level;
        Height = height;
        PathPoints = pathPoints ?? new List<JZPoint>();
        Options = new Dictionary<string, object>();
    }

    /// <summary>
    ///     Start point of the railing
    /// </summary>
    [JsonProperty("startPoint")]
    public JZPoint StartPoint { get; set; }

    /// <summary>
    ///     End point of the railing
    /// </summary>
    [JsonProperty("endPoint")]
    public JZPoint EndPoint { get; set; }

    /// <summary>
    ///     Path points defining the railing path
    /// </summary>
    [JsonProperty("pathPoints")]
    public List<JZPoint> PathPoints { get; set; }

    /// <summary>
    ///     Base level elevation (mm)
    /// </summary>
    [JsonProperty("level")]
    public double Level { get; set; }

    /// <summary>
    ///     Level offset (mm)
    /// </summary>
    [JsonProperty("levelOffset")]
    public double LevelOffset { get; set; }

    /// <summary>
    ///     Railing height (mm)
    /// </summary>
    [JsonProperty("height")]
    public double Height { get; set; } = 1070;

    /// <summary>
    ///     Host element ID for the railing (if attaching to a stair, ramp, etc.)
    /// </summary>
    [JsonProperty("hostElementId")]
    public int HostElementId { get; set; } = -1;

    /// <summary>
    ///     Is this a closed loop railing
    /// </summary>
    [JsonProperty("isClosedLoop")]
    public bool IsClosedLoop { get; set; }

    /// <summary>
    ///     Railing type ID in Revit
    /// </summary>
    [JsonProperty("typeId")]
    public int TypeId { get; set; } = -1;

    /// <summary>
    ///     Railing type name
    /// </summary>
    [JsonProperty("railingType")]
    public string RailingType { get; set; } = "Standard";

    /// <summary>
    ///     Railing material
    /// </summary>
    [JsonProperty("material")]
    public string Material { get; set; } = "Metal";

    /// <summary>
    ///     Additional options
    /// </summary>
    [JsonProperty("options")]
    public Dictionary<string, object> Options { get; set; }
}