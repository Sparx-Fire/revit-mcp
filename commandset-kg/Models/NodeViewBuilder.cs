using System.Collections.Generic;
using System.Linq;
using RevitMCPKgCommandSet.Core;

namespace RevitMCPKgCommandSet.Models
{
    public static class NodeViewBuilder
    {
        // `select` (optional): when non-empty, only those attr keys are
        // emitted (field projection). Structural fields (llm_id, node_type,
        // revit_id, lifecycle turns) are always kept — the payload bloat is
        // in attrs (p1/p2 arrays etc.), so projecting attrs is the win.
        public static KgNodeView From(Node node, HashSet<string> select = null)
        {
            if (node == null) return null;
            bool project = select != null && select.Count > 0;
            var publicAttrs = new Dictionary<string, object>();
            foreach (var kvp in node.Attrs)
            {
                if (LifecycleAttrs.Reserved.Contains(kvp.Key)) continue;
                if (project && !select.Contains(kvp.Key)) continue;
                publicAttrs[kvp.Key] = kvp.Value;
            }

            return new KgNodeView
            {
                LlmId = node.LlmId,
                NodeType = node.NodeType,
                RevitId = node.RevitId,
                CreatedAtTurn = node.CreatedAtTurn,
                ModifiedAtTurn = node.ModifiedAtTurns,
                DeletedAtTurn = node.DeletedAtTurn,
                Attrs = publicAttrs,
            };
        }

        public static List<KgNodeView> FromMany(IEnumerable<Node> nodes, HashSet<string> select = null) =>
            nodes.Select(n => From(n, select)).ToList();
    }
}
