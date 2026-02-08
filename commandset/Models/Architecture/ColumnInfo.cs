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
///     Information about a structural column
/// </summary>
public class ColumnInfo
{
    /// <summary>
    ///     Default constructor
    /// </summary>
    public ColumnInfo()
    {
        Options = new Dictionary<string, object>();
    }

    /// <summary>
    ///     Constructor with position and dimensions
    /// </summary>
    /// <param name="x">X position (mm)</param>
    /// <param name="y">Y position (mm)</param>
    /// <param name="z">Z position (mm)</param>
    /// <param name="width">Width (mm)</param>
    /// <param name="depth">Depth (mm)</param>
    /// <param name="height">Height (mm)</param>
    /// <param name="baseLevel">Base level elevation (mm)</param>
    public ColumnInfo(double x, double y, double z, double width, double depth, double height, double baseLevel)
    {
        Location = new JZPoint(x, y, z);
        Width = width;
        Depth = depth;
        Height = height;
        BaseLevel = baseLevel;
    }

    /// <summary>
    ///     Column position (mm)
    /// </summary>
    [JsonProperty("location")]
    public JZPoint Location { get; set; }

    /// <summary>
    ///     Column width (mm)
    /// </summary>
    [JsonProperty("width")]
    public double Width { get; set; }

    /// <summary>
    ///     Column depth (mm)
    /// </summary>
    [JsonProperty("depth")]
    public double Depth { get; set; }

    /// <summary>
    ///     Column height (mm)
    /// </summary>
    [JsonProperty("height")]
    public double Height { get; set; }

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
    ///     Column rotation (degrees)
    /// </summary>
    [JsonProperty("rotation")]
    public double Rotation { get; set; }

    /// <summary>
    ///     Column type
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; }

    /// <summary>
    ///     Column family type ID in Revit
    /// </summary>
    [JsonProperty("typeId")]
    public int TypeId { get; set; }

    /// <summary>
    ///     Column material
    /// </summary>
    [JsonProperty("material")]
    public string Material { get; set; }

    /// <summary>
    ///     Column diameter (for circular columns, mm)
    /// </summary>
    [JsonProperty("diameter")]
    public double Diameter { get; set; }

    /// <summary>
    ///     Is structural
    /// </summary>
    [JsonProperty("isStructural")]
    public bool IsStructural { get; set; }

    /// <summary>
    ///     Additional options
    /// </summary>
    [JsonProperty("options")]
    public Dictionary<string, object> Options { get; set; }
}