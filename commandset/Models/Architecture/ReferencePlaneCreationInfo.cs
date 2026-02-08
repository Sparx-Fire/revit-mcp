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
///     Information for reference plane creation
/// </summary>
public class ReferencePlaneCreationInfo
{
    /// <summary>
    ///     Default constructor
    /// </summary>
    public ReferencePlaneCreationInfo()
    {
        Points = new List<Point>();
        Parameters = new Dictionary<string, object>();
    }

    /// <summary>
    ///     Creation method (ByLine, ByNormal, ByPoints)
    /// </summary>
    [JsonProperty("creationMethod")]
    public string CreationMethod { get; set; } = "ByLine";

    /// <summary>
    ///     Name for the reference plane
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     Bubble end point for line-based creation (millimeters)
    /// </summary>
    [JsonProperty("bubbleEnd")]
    public Point BubbleEnd { get; set; }

    /// <summary>
    ///     Free end point for line-based creation (millimeters)
    /// </summary>
    [JsonProperty("freeEnd")]
    public Point FreeEnd { get; set; }

    /// <summary>
    ///     Third point for defining the plane (millimeters)
    /// </summary>
    [JsonProperty("thirdPoint")]
    public Point ThirdPoint { get; set; }

    /// <summary>
    ///     Origin point for normal-based creation (millimeters)
    /// </summary>
    [JsonProperty("origin")]
    public Point Origin { get; set; }

    /// <summary>
    ///     Normal vector for normal-based creation
    /// </summary>
    [JsonProperty("normal")]
    public Point Normal { get; set; }

    /// <summary>
    ///     Length of the reference plane line in millimeters
    /// </summary>
    [JsonProperty("length")]
    public double Length { get; set; }

    /// <summary>
    ///     Points for creating reference plane (for ByPoints method)
    /// </summary>
    [JsonProperty("points")]
    public List<Point> Points { get; set; }

    /// <summary>
    ///     Additional parameters
    /// </summary>
    [JsonProperty("parameters")]
    public Dictionary<string, object> Parameters { get; set; }
}