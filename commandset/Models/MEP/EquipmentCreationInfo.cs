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

namespace RevitMCPCommandSet.Models.MEP;

/// <summary>
///     Information about equipment creation parameters
/// </summary>
public class EquipmentCreationInfo
{
    /// <summary>
    ///     Default constructor
    /// </summary>
    public EquipmentCreationInfo()
    {
        Location = new JZPoint(0, 0, 0);
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
    /// <param name="level">Base level elevation (mm)</param>
    public EquipmentCreationInfo(double x, double y, double z, double width, double depth, double height,
        double level = 0)
    {
        Location = new JZPoint(x, y, z);
        Width = width;
        Depth = depth;
        Height = height;
        BaseLevel = level;
    }

    /// <summary>
    ///     Equipment position (mm)
    /// </summary>
    [JsonProperty("location")]
    public JZPoint Location { get; set; }

    /// <summary>
    ///     Equipment rotation around Z-axis (degrees)
    /// </summary>
    [JsonProperty("rotation")]
    public double Rotation { get; set; }

    /// <summary>
    ///     Equipment width (mm)
    /// </summary>
    [JsonProperty("width")]
    public double Width { get; set; }

    /// <summary>
    ///     Equipment depth (mm)
    /// </summary>
    [JsonProperty("depth")]
    public double Depth { get; set; }

    /// <summary>
    ///     Equipment height (mm)
    /// </summary>
    [JsonProperty("height")]
    public double Height { get; set; }

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
    ///     Equipment category
    /// </summary>
    [JsonProperty("category")]
    public string Category { get; set; } =
        "Mechanical Equipment"; // Mechanical Equipment, Electrical Equipment, Plumbing Fixtures

    /// <summary>
    ///     Equipment type
    /// </summary>
    [JsonProperty("equipmentType")]
    public string EquipmentType { get; set; } // AHU, Pump, Fan, Panel, etc.

    /// <summary>
    ///     Equipment system
    /// </summary>
    [JsonProperty("system")]
    public string System { get; set; } // HVAC, Electrical, Plumbing, Fire Protection

    /// <summary>
    ///     Equipment family name
    /// </summary>
    [JsonProperty("familyName")]
    public string FamilyName { get; set; }

    /// <summary>
    ///     Equipment type ID in Revit
    /// </summary>
    [JsonProperty("typeId")]
    public int TypeId { get; set; }

    /// <summary>
    ///     Is electrical connected
    /// </summary>
    [JsonProperty("isElectricalConnected")]
    public bool IsElectricalConnected { get; set; }

    /// <summary>
    ///     Requires ventilation
    /// </summary>
    [JsonProperty("requiresVentilation")]
    public bool RequiresVentilation { get; set; }

    /// <summary>
    ///     Equipment power rating (watts)
    /// </summary>
    [JsonProperty("powerRating")]
    public double PowerRating { get; set; }

    /// <summary>
    ///     Additional options
    /// </summary>
    [JsonProperty("options")]
    public Dictionary<string, object> Options { get; set; }
}