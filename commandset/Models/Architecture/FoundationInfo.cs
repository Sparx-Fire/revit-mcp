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

public class FoundationInfo
{
    /// <summary>
    ///     Type of foundation (e.g., "Isolated", "Strip", "Raft", "Pile")
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = "Isolated";

    /// <summary>
    ///     Location of the foundation in millimeters (X, Y, Z coordinates)
    /// </summary>
    [JsonProperty("location")]
    public JZPoint Location { get; set; } = new();

    /// <summary>
    ///     Width of the foundation in millimeters
    /// </summary>
    [JsonProperty("width")]
    public double Width { get; set; }

    /// <summary>
    ///     Length of the foundation in millimeters
    /// </summary>
    [JsonProperty("length")]
    public double Length { get; set; }

    /// <summary>
    ///     Thickness/depth of the foundation in millimeters
    /// </summary>
    [JsonProperty("thickness")]
    public double Thickness { get; set; }

    /// <summary>
    ///     Elevation of the foundation in millimeters
    /// </summary>
    [JsonProperty("elevation")]
    public double Elevation { get; set; }

    /// <summary>
    ///     Material of the foundation
    /// </summary>
    [JsonProperty("material")]
    public string Material { get; set; }

    /// <summary>
    ///     Optional ID of the column or wall supported by this foundation
    /// </summary>
    [JsonProperty("supportedElementId")]
    public int SupportedElementId { get; set; }

    /// <summary>
    ///     Optional rotation angle in degrees
    /// </summary>
    [JsonProperty("rotation")]
    public double Rotation { get; set; }

    /// <summary>
    ///     Optional parameter for strip foundation - path points
    /// </summary>
    [JsonProperty("path")]
    public List<JZPoint> Path { get; set; } = new();

    /// <summary>
    ///     Optional parameter for pile foundation - depth
    /// </summary>
    [JsonProperty("depth")]
    public double Depth { get; set; }

    /// <summary>
    ///     Optional parameter for pile foundation - diameter
    /// </summary>
    [JsonProperty("diameter")]
    public double Diameter { get; set; }

    /// <summary>
    ///     Optional parameter for raft foundation - boundary points
    /// </summary>
    [JsonProperty("boundary")]
    public List<JZPoint> Boundary { get; set; } = new();

    /// <summary>
    ///     Optional parameter for family type ID
    /// </summary>
    [JsonProperty("typeId")]
    public int TypeId { get; set; }
}