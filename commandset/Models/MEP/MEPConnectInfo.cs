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

namespace RevitMCPCommandSet.Models.MEP;

/// <summary>
///     Information for MEP connector connection parameters
/// </summary>
public class MEPConnectInfo
{
    /// <summary>
    ///     Default constructor
    /// </summary>
    public MEPConnectInfo()
    {
        Options = new Dictionary<string, object>();
    }

    /// <summary>
    ///     First element ID
    /// </summary>
    [JsonProperty("elementId1")]
    public int ElementId1 { get; set; }

    /// <summary>
    ///     Second element ID
    /// </summary>
    [JsonProperty("elementId2")]
    public int ElementId2 { get; set; }

    /// <summary>
    ///     Connector index on first element (optional)
    /// </summary>
    [JsonProperty("connectorIndex1")]
    public int ConnectorIndex1 { get; set; } = -1;

    /// <summary>
    ///     Connector index on second element (optional)
    /// </summary>
    [JsonProperty("connectorIndex2")]
    public int ConnectorIndex2 { get; set; } = -1;

    /// <summary>
    ///     Connection type (Direct/Elbow/Tee/Reducer/Cross)
    /// </summary>
    [JsonProperty("connectType")]
    public string ConnectType { get; set; } = "Direct";

    /// <summary>
    ///     Additional options
    /// </summary>
    [JsonProperty("options")]
    public Dictionary<string, object> Options { get; set; }
}
