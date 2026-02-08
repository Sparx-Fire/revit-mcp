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
///     Information for group creation from elements
/// </summary>
public class GroupCreationInfo
{
    /// <summary>
    ///     Default constructor
    /// </summary>
    public GroupCreationInfo()
    {
        ElementIds = new List<int>();
    }

    /// <summary>
    ///     Name for the new group
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     Element IDs to include in the group
    /// </summary>
    [JsonProperty("elementIds")]
    public List<int> ElementIds { get; set; }
}

/// <summary>
///     Information for placing a group instance
/// </summary>
public class GroupInstanceInfo
{
    /// <summary>
    ///     Group type ID to place
    /// </summary>
    [JsonProperty("groupTypeId")]
    public int GroupTypeId { get; set; }

    /// <summary>
    ///     Insertion point for the group (millimeters)
    /// </summary>
    [JsonProperty("insertionPoint")]
    public Point InsertionPoint { get; set; }

    /// <summary>
    ///     Rotation angle in degrees
    /// </summary>
    [JsonProperty("rotation")]
    public double Rotation { get; set; }

    /// <summary>
    ///     Mirror the group about X axis
    /// </summary>
    [JsonProperty("mirrorAboutX")]
    public bool MirrorAboutX { get; set; }

    /// <summary>
    ///     Mirror the group about Y axis
    /// </summary>
    [JsonProperty("mirrorAboutY")]
    public bool MirrorAboutY { get; set; }
}

/// <summary>
///     Result of group creation or placement
/// </summary>
public class GroupResult
{
    /// <summary>
    ///     Group instance ID
    /// </summary>
    [JsonProperty("groupId")]
    public int GroupId { get; set; }

    /// <summary>
    ///     Group type ID
    /// </summary>
    [JsonProperty("groupTypeId")]
    public int GroupTypeId { get; set; }

    /// <summary>
    ///     Group name
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;
}