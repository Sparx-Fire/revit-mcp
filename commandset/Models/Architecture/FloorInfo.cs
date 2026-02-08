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
///     Information about a floor in Revit, used for both creation and retrieval
/// </summary>
public class FloorInfo
{
    /// <summary>
    ///     Default constructor
    /// </summary>
    public FloorInfo()
    {
        BoundaryPoints = new List<JZPoint>();
        Openings = new List<List<JZPoint>>();
        Options = new Dictionary<string, object>();
    }

    /// <summary>
    ///     Constructor with basic parameters
    /// </summary>
    /// <param name="level">Base level elevation (mm)</param>
    /// <param name="thickness">Floor thickness (mm)</param>
    /// <param name="boundaryPoints">List of boundary points forming floor outline</param>
    public FloorInfo(double level, double thickness, List<JZPoint> boundaryPoints)
    {
        Level = level;
        Thickness = thickness;
        BoundaryPoints = boundaryPoints ?? new List<JZPoint>();
        Openings = new List<List<JZPoint>>();
        Options = new Dictionary<string, object>();
    }

    /// <summary>
    ///     Element ID
    /// </summary>
    [JsonProperty("elementId")]
    public int ElementId { get; set; }

    /// <summary>
    ///     Floor name
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; set; } = "Floor";

    /// <summary>
    ///     Base level elevation (mm)
    /// </summary>
    [JsonProperty("level")]
    public double Level { get; set; }

    /// <summary>
    ///     Floor height (mm) - the vertical dimension of the floor space
    /// </summary>
    [JsonProperty("height")]
    public double Height { get; set; }

    /// <summary>
    ///     Floor thickness (mm) - the physical thickness of the floor element
    /// </summary>
    [JsonProperty("thickness")]
    public double Thickness { get; set; }

    /// <summary>
    ///     Floor boundary points (mm)
    /// </summary>
    [JsonProperty("boundaryPoints")]
    public List<JZPoint> BoundaryPoints { get; set; }

    /// <summary>
    ///     Holes/openings in the floor (list of boundary loops)
    /// </summary>
    [JsonProperty("openings")]
    public List<List<JZPoint>> Openings { get; set; }

    /// <summary>
    ///     Level offset (mm)
    /// </summary>
    [JsonProperty("levelOffset")]
    public double LevelOffset { get; set; }

    /// <summary>
    ///     Floor type ID in Revit
    /// </summary>
    [JsonProperty("typeId")]
    public int TypeId { get; set; } = -1;

    /// <summary>
    ///     Floor type name
    /// </summary>
    [JsonProperty("floorType")]
    public string FloorType { get; set; }

    /// <summary>
    ///     Floor material
    /// </summary>
    [JsonProperty("material")]
    public string Material { get; set; }

    /// <summary>
    ///     Floor level name
    /// </summary>
    [JsonProperty("levelName")]
    public string LevelName { get; set; }

    /// <summary>
    ///     Is floor structural
    /// </summary>
    [JsonProperty("isStructural")]
    public bool IsStructural { get; set; }

    /// <summary>
    ///     Floor area (m²)
    /// </summary>
    [JsonProperty("area")]
    public double Area { get; set; }

    /// <summary>
    ///     Additional options
    /// </summary>
    [JsonProperty("options")]
    public Dictionary<string, object> Options { get; set; }
}