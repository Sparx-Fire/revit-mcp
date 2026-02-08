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

namespace RevitMCPCommandSet.Models.Architecture;

/// <summary>
///     Information about the building roof
/// </summary>
public class RoofInfo
{
    /// <summary>
    ///     Default constructor
    /// </summary>
    public RoofInfo()
    {
        Options = new Dictionary<string, object>();
    }

    /// <summary>
    ///     Constructor with basic parameters
    /// </summary>
    /// <param name="type">Roof type</param>
    /// <param name="level">Roof elevation (mm)</param>
    /// <param name="thickness">Roof thickness (mm)</param>
    /// <param name="material">Roof material</param>
    public RoofInfo(string type, double level, double thickness, string material = "Concrete")
    {
        Type = type;
        Level = level;
        Thickness = thickness;
        Material = material;

        Options = new Dictionary<string, object>();
    }

    /// <summary>
    ///     Roof type
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } // Flat, Gable, Hip, etc.

    /// <summary>
    ///     Roof elevation (mm)
    /// </summary>
    [JsonProperty("level")]
    public double Level { get; set; }

    /// <summary>
    ///     Roof height (for pitched roofs) (mm)
    /// </summary>
    [JsonProperty("height")]
    public double Height { get; set; }

    /// <summary>
    ///     Roof thickness (mm)
    /// </summary>
    [JsonProperty("thickness")]
    public double Thickness { get; set; }

    /// <summary>
    ///     Roof slope (degrees)
    /// </summary>
    [JsonProperty("slope")]
    public double Slope { get; set; }

    /// <summary>
    ///     Roof overhang from walls (mm)
    /// </summary>
    [JsonProperty("overhang")]
    public double Overhang { get; set; }

    /// <summary>
    ///     Roof material
    /// </summary>
    [JsonProperty("material")]
    public string Material { get; set; }

    /// <summary>
    ///     Additional options
    /// </summary>
    [JsonProperty("options")]
    public Dictionary<string, object> Options { get; set; } = new();
}