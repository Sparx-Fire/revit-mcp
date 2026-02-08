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

namespace RevitMCPCommandSet.Models.Views;

/// <summary>
///     Information for viewport creation
/// </summary>
public class ViewportCreationInfo
{
    /// <summary>
    ///     Default constructor
    /// </summary>
    public ViewportCreationInfo()
    {
        Parameters = new Dictionary<string, object>();
    }

    /// <summary>
    ///     Sheet ID to place viewport on
    /// </summary>
    [JsonProperty("sheetId")]
    public int SheetId { get; set; }

    /// <summary>
    ///     View ID to place in viewport
    /// </summary>
    [JsonProperty("viewId")]
    public int ViewId { get; set; }

    /// <summary>
    ///     X position on sheet in millimeters
    /// </summary>
    [JsonProperty("positionX")]
    public double PositionX { get; set; }

    /// <summary>
    ///     Y position on sheet in millimeters
    /// </summary>
    [JsonProperty("positionY")]
    public double PositionY { get; set; }

    /// <summary>
    ///     Viewport type ID
    /// </summary>
    [JsonProperty("viewportTypeId")]
    public int ViewportTypeId { get; set; }

    /// <summary>
    ///     Whether to display the view title
    /// </summary>
    [JsonProperty("displayTitle")]
    public bool? DisplayTitle { get; set; }

    /// <summary>
    ///     Override scale for the viewport
    /// </summary>
    [JsonProperty("scaleOverride")]
    public int ScaleOverride { get; set; }

    /// <summary>
    ///     Viewport label text
    /// </summary>
    [JsonProperty("labelText")]
    public string LabelText { get; set; } = string.Empty;

    /// <summary>
    ///     Rotation angle in degrees
    /// </summary>
    [JsonProperty("rotation")]
    public double Rotation { get; set; }

    /// <summary>
    ///     Additional parameters
    /// </summary>
    [JsonProperty("parameters")]
    public Dictionary<string, object> Parameters { get; set; }
}