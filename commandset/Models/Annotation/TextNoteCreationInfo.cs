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

namespace RevitMCPCommandSet.Models.Annotation;

/// <summary>
///     Information about text note creation parameters
/// </summary>
public class TextNoteCreationInfo
{
    /// <summary>
    ///     Default constructor
    /// </summary>
    public TextNoteCreationInfo()
    {
        Location = new JZPoint(0, 0, 0);
        Options = new Dictionary<string, object>();
    }

    /// <summary>
    ///     Text note location point (mm)
    /// </summary>
    [JsonProperty("location")]
    public JZPoint Location { get; set; }

    /// <summary>
    ///     Text content
    /// </summary>
    [JsonProperty("text")]
    public string Text { get; set; } = "";

    /// <summary>
    ///     Text rotation in degrees
    /// </summary>
    [JsonProperty("rotation")]
    public double Rotation { get; set; }

    /// <summary>
    ///     Text width (mm) - if zero, no width limit
    /// </summary>
    [JsonProperty("width")]
    public double Width { get; set; }

    /// <summary>
    ///     Text note type ID
    /// </summary>
    [JsonProperty("textNoteTypeId")]
    public int TextNoteTypeId { get; set; } = -1;

    /// <summary>
    ///     View ID - view to create text note in
    /// </summary>
    [JsonProperty("viewId")]
    public int ViewId { get; set; } = -1;

    /// <summary>
    ///     Text horizontal alignment (Left=0, Center=1, Right=2)
    /// </summary>
    [JsonProperty("horizontalAlign")]
    public int HorizontalAlign { get; set; }

    /// <summary>
    ///     Text vertical alignment (Top=0, Middle=1, Bottom=2)
    /// </summary>
    [JsonProperty("verticalAlign")]
    public int VerticalAlign { get; set; }

    /// <summary>
    ///     Additional options
    /// </summary>
    [JsonProperty("options")]
    public Dictionary<string, object> Options { get; set; }
}
