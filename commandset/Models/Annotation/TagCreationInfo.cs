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
///     Information about tag creation parameters
/// </summary>
public class TagCreationInfo
{
    /// <summary>
    ///     Default constructor
    /// </summary>
    public TagCreationInfo()
    {
        Location = new JZPoint(0, 0, 0);
        Options = new Dictionary<string, object>();
    }

    /// <summary>
    ///     Tag location point (mm)
    /// </summary>
    [JsonProperty("location")]
    public JZPoint Location { get; set; }

    /// <summary>
    ///     Element ID to tag
    /// </summary>
    [JsonProperty("elementId")]
    public int ElementId { get; set; } = -1;

    /// <summary>
    ///     Tag orientation (horizontal=0, vertical=1)
    /// </summary>
    [JsonProperty("orientation")]
    public int Orientation { get; set; }

    /// <summary>
    ///     Tag rotation in degrees
    /// </summary>
    [JsonProperty("rotation")]
    public double Rotation { get; set; }

    /// <summary>
    ///     Is tag leader visible
    /// </summary>
    [JsonProperty("hasLeader")]
    public bool HasLeader { get; set; }

    /// <summary>
    ///     Tag type ID
    /// </summary>
    [JsonProperty("tagTypeId")]
    public int TagTypeId { get; set; } = -1;

    /// <summary>
    ///     Tag category (Door, Window, Wall, etc.)
    /// </summary>
    [JsonProperty("tagCategory")]
    public string TagCategory { get; set; } = "";

    /// <summary>
    ///     View ID - view to create tag in
    /// </summary>
    [JsonProperty("viewId")]
    public int ViewId { get; set; } = -1;

    /// <summary>
    ///     Additional options
    /// </summary>
    [JsonProperty("options")]
    public Dictionary<string, object> Options { get; set; }
}