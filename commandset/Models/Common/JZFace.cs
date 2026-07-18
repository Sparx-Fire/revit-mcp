using Newtonsoft.Json;

namespace RevitMCPCommandSet.Models.Common;

/// <summary>
/// </summary>
public class JZFace
{
    /// <summary>
    /// </summary>
    public JZFace()
    {
        InnerLoops = new List<List<JZLine>>();
        OuterLoop = new List<JZLine>();
    }

    /// <summary>
    /// </summary>
    [JsonProperty("outerLoop")]
    public List<JZLine> OuterLoop { get; set; }

    /// <summary>
    /// </summary>
    [JsonProperty("innerLoops")]
    public List<List<JZLine>> InnerLoops { get; set; }
}