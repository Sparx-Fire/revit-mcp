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
///     Information for sheet creation
/// </summary>
public class SheetCreationInfo
{
    /// <summary>
    ///     Default constructor
    /// </summary>
    public SheetCreationInfo()
    {
        Parameters = new Dictionary<string, object>();
        RevisionIds = new List<int>();
    }

    /// <summary>
    ///     Sheet number
    /// </summary>
    [JsonProperty("sheetNumber")]
    public string SheetNumber { get; set; } = string.Empty;

    /// <summary>
    ///     Sheet name
    /// </summary>
    [JsonProperty("sheetName")]
    public string SheetName { get; set; } = string.Empty;

    /// <summary>
    ///     Title block type ID
    /// </summary>
    [JsonProperty("titleBlockTypeId")]
    public int TitleBlockTypeId { get; set; }

    /// <summary>
    ///     Title block family name
    /// </summary>
    [JsonProperty("titleBlockFamilyName")]
    public string TitleBlockFamilyName { get; set; } = string.Empty;

    /// <summary>
    ///     Title block type name
    /// </summary>
    [JsonProperty("titleBlockTypeName")]
    public string TitleBlockTypeName { get; set; } = string.Empty;

    /// <summary>
    ///     Revision IDs to apply to the sheet
    /// </summary>
    [JsonProperty("revisionIds")]
    public List<int> RevisionIds { get; set; }

    /// <summary>
    ///     Additional parameters
    /// </summary>
    [JsonProperty("parameters")]
    public Dictionary<string, object> Parameters { get; set; }
}

/// <summary>
///     Result of sheet creation
/// </summary>
public class SheetCreationResult
{
    [JsonProperty("success")]
    public bool Success { get; set; }

    [JsonProperty("message")]
    public string Message { get; set; } = string.Empty;

    [JsonProperty("sheetId")]
    public long SheetId { get; set; }

    [JsonProperty("sheetUniqueId")]
    public string SheetUniqueId { get; set; } = string.Empty;

    [JsonProperty("sheetNumber")]
    public string SheetNumber { get; set; } = string.Empty;

    [JsonProperty("sheetName")]
    public string SheetName { get; set; } = string.Empty;

    [JsonProperty("titleBlockTypeId")]
    public long TitleBlockTypeId { get; set; }

    [JsonProperty("titleBlockFamilyName")]
    public string TitleBlockFamilyName { get; set; } = string.Empty;

    [JsonProperty("titleBlockTypeName")]
    public string TitleBlockTypeName { get; set; } = string.Empty;

    [JsonProperty("warnings")]
    public List<string> Warnings { get; set; } = new List<string>();
}