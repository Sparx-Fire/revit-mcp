using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitMCPCommandSet.Models.Common
{
    /// <summary>
    /// </summary>
    public enum ElementOperationType
    {
        /// <summary>
        /// </summary>
        Select,

        /// <summary>
        /// </summary>
        SelectionBox,

        /// <summary>
        /// </summary>
        SetColor,

        /// <summary>
        /// </summary>
        SetTransparency,

        /// <summary>
        /// </summary>
        Delete,

        /// <summary>
        /// </summary>
        Hide,

        /// <summary>
        /// </summary>
        TempHide,

        /// <summary>
        /// </summary>
        Isolate,

        /// <summary>
        /// </summary>
        Unhide,

        /// <summary>
        /// </summary>
        ResetIsolate,
    }


    /// <summary>
    /// </summary>
    public class OperationSetting
    {
        /// <summary>
        /// </summary>
        [JsonProperty("elementIds")]
        public List<int> ElementIds = new List<int>();

        /// <summary>
        /// </summary>
        [JsonProperty("action")]
        public string Action { get; set; } = "Select";

        /// <summary>
        /// </summary>
        [JsonProperty("transparencyValue")]
        public int TransparencyValue { get; set; } = 50;

        /// <summary>
        /// </summary>
        [JsonProperty("colorValue")]
        public int[] ColorValue { get; set; } = new int[] { 255, 0, 0 }; // Default red
    }
}
