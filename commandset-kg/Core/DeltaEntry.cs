using System.Collections.Generic;

namespace RevitMCPKgCommandSet.Core
{
    public static class DeltaOps
    {
        public const string CreateNode = "create_node";
        // User-authored semantic node (not derived from Revit). Free-form
        // attrs, no schema. Replay re-registers the type before adding.
        public const string CreateUserNode = "create_user_node";
        public const string ModifyNode = "modify_node";
        public const string SoftDelete = "soft_delete";
        public const string Resurrect = "resurrect";
        public const string SetRevitId = "set_revit_id";
        public const string AddEdge = "add_edge";
        public const string RemoveEdge = "remove_edge";
        public const string AdvanceTurn = "advance_turn";
        public const string Annotate = "annotate";
    }

    public sealed class DeltaEntry
    {
        public int Turn { get; set; }
        public string Op { get; set; }

        public string Id { get; set; }
        public string NodeType { get; set; }
        public Dictionary<string, object> Attrs { get; set; }
        public Dictionary<string, object> Updates { get; set; }
        public long? RevitId { get; set; }
        public string Src { get; set; }
        public string Dst { get; set; }
        public string EdgeType { get; set; }
    }
}
