using Newtonsoft.Json;
using System.Collections.Generic;

namespace RevitMCPCommandSet.Models.Common
{
    /// <summary>
    /// One parameter set on one element.
    /// </summary>
    public class BatchSetParameterOperation
    {
        /// <summary>Revit ElementId (Int64).</summary>
        [JsonProperty("element_id")]
        public long ElementId { get; set; }

        /// <summary>
        /// Parameter identifier. Resolved with priority :
        ///   1. BuiltInParameter enum name (e.g. "WALL_USER_HEIGHT_PARAM").
        ///   2. Element.LookupParameter(string), case-insensitive fallback.
        /// The first that resolves wins. Locale-friendly via LookupParameter.
        /// </summary>
        [JsonProperty("param")]
        public string Param { get; set; }

        /// <summary>
        /// New value. Type matches the parameter's StorageType :
        ///   Double   : if length-typed, value is expected in METRES (auto
        ///              converted to feet internally); otherwise raw double.
        ///   Integer  : int (cast from JSON number).
        ///   String   : string.
        ///   ElementId: long (cast from JSON number → ElementId).
        /// </summary>
        [JsonProperty("value")]
        public object Value { get; set; }
    }

    public class BatchSetParametersSetting
    {
        [JsonProperty("operations")]
        public List<BatchSetParameterOperation> Operations { get; set; }
            = new List<BatchSetParameterOperation>();

        /// <summary>
        /// If true (default), any per-op failure rolls back the whole batch
        /// — no partial state mutation. If false, best-effort : failed ops
        /// are skipped and reported; succeeded ops commit.
        /// </summary>
        [JsonProperty("atomic")]
        public bool Atomic { get; set; } = true;
    }

    public class BatchSetParameterError
    {
        [JsonProperty("index")]
        public int Index { get; set; }

        [JsonProperty("element_id")]
        public long ElementId { get; set; }

        [JsonProperty("param")]
        public string Param { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }

    public class BatchSetParametersResult
    {
        [JsonProperty("total")]
        public int Total { get; set; }

        [JsonProperty("succeeded")]
        public int Succeeded { get; set; }

        [JsonProperty("failed")]
        public int Failed { get; set; }

        [JsonProperty("rolled_back")]
        public bool RolledBack { get; set; }

        [JsonProperty("errors")]
        public List<BatchSetParameterError> Errors { get; set; }
            = new List<BatchSetParameterError>();
    }
}
