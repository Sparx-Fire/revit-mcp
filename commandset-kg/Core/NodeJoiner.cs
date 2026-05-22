using System.Collections.Generic;
using System.Linq;

namespace RevitMCPKgCommandSet.Core
{
    public sealed class JoinStep
    {
        public string EdgeType { get; set; }
        public EdgeDirection Direction { get; set; }
        public List<string> Select { get; set; }
        public string As { get; set; }
    }

    // Edge-aware join projection: resolves a chained traversal server-side
    // and returns one flat row per start node, so a relational audit (e.g.
    // window -> host wall -> level) is ONE call instead of N traversals.
    // Single-valued hops (takes the first neighbor) — fits 1:1 structural
    // edges like hosts / at_level / is_type.
    public static class NodeJoiner
    {
        public static List<Dictionary<string, object>> BuildRows(
            ProjectKg kg,
            IEnumerable<Node> nodes,
            HashSet<string> select,
            List<JoinStep> join)
        {
            var rows = new List<Dictionary<string, object>>();
            if (kg == null) return rows;

            foreach (var node in nodes)
            {
                var row = new Dictionary<string, object> { ["llm_id"] = node.LlmId };
                if (select != null && select.Count > 0)
                    foreach (var k in select)
                        if (node.Attrs.TryGetValue(k, out var v)) row[k] = v;

                string currentId = node.LlmId;
                foreach (var step in join)
                {
                    var neighbor = currentId == null ? null : FirstNeighbor(kg, currentId, step);
                    row[step.As + "_id"] = neighbor?.LlmId;
                    if (step.Select != null && neighbor != null)
                        foreach (var k in step.Select)
                            row[step.As + "_" + k] =
                                neighbor.Attrs.TryGetValue(k, out var v) ? v : null;
                    currentId = neighbor?.LlmId;
                }
                rows.Add(row);
            }
            return rows;
        }

        private static Node FirstNeighbor(ProjectKg kg, string id, JoinStep step)
        {
            string neighborId = step.Direction == EdgeDirection.Outgoing
                ? kg.OutgoingEdges(id, step.EdgeType).FirstOrDefault()?.Dst
                : kg.IncomingEdges(id, step.EdgeType).FirstOrDefault()?.Src;
            if (neighborId == null || !kg.HasNode(neighborId)) return null;
            return kg.GetNode(neighborId);
        }
    }
}
