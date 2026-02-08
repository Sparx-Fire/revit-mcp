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
///     Overall description of a building
/// </summary>
public class BuildingInfo
{
    /// <summary>
    ///     Default constructor
    /// </summary>
    public BuildingInfo()
    {
        // Initialize roof information
        Roof = new RoofInfo();
    }

    /// <summary>
    ///     Create a basic building structure with given dimensions
    /// </summary>
    /// <param name="width">Width (mm)</param>
    /// <param name="length">Length (mm)</param>
    /// <param name="height">Height (mm)</param>
    /// <param name="floorCount">Number of floors</param>
    public BuildingInfo(double width, double length, double height, int floorCount = 1)
    {
        Width = width;
        Length = length;
        Height = height;
        FloorCount = floorCount;
    }

    /// <summary>
    ///     Building name
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; set; } = "Building";

    /// <summary>
    ///     Building description
    /// </summary>
    [JsonProperty("description")]
    public string Description { get; set; } = "";

    /// <summary>
    ///     Overall width (mm)
    /// </summary>
    [JsonProperty("width")]
    public double Width { get; set; }

    /// <summary>
    ///     Overall length (mm)
    /// </summary>
    [JsonProperty("length")]
    public double Length { get; set; }

    /// <summary>
    ///     Overall height (mm)
    /// </summary>
    [JsonProperty("height")]
    public double Height { get; set; }

    /// <summary>
    ///     Number of floors
    /// </summary>
    [JsonProperty("floorCount")]
    public int FloorCount { get; set; }

    /// <summary>
    ///     Height per floor (mm)
    /// </summary>
    [JsonProperty("floorHeight")]
    public double FloorHeight { get; set; }

    /// <summary>
    ///     Roof type
    /// </summary>
    [JsonProperty("roofType")]
    public string RoofType { get; set; } // Flat, Gable, Hip, etc.

    /// <summary>
    ///     Information about floors
    /// </summary>
    [JsonProperty("floors")]
    public List<FloorInfo> Floors { get; set; } = new();

    /// <summary>
    ///     List of walls
    /// </summary>
    [JsonProperty("walls")]
    public List<LineElement> Walls { get; set; } = new();

    /// <summary>
    ///     List of doors
    /// </summary>
    [JsonProperty("doors")]
    public List<DoorInfo> Doors { get; set; } = new();

    /// <summary>
    ///     List of windows
    /// </summary>
    [JsonProperty("windows")]
    public List<WindowInfo> Windows { get; set; } = new();

    /// <summary>
    ///     List of balconies
    /// </summary>
    [JsonProperty("balconies")]
    public List<BalconyInfo> Balconies { get; set; } = new();

    /// <summary>
    ///     Roof information
    /// </summary>
    [JsonProperty("roof")]
    public RoofInfo Roof { get; set; }

    /// <summary>
    ///     Materials for components
    /// </summary>
    [JsonProperty("materials")]
    public Dictionary<string, string> Materials { get; set; } = new();

    /// <summary>
    ///     Additional options
    /// </summary>
    [JsonProperty("options")]
    public Dictionary<string, object> Options { get; set; } = new();
}