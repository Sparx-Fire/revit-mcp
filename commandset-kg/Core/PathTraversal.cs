using System.Collections.Generic;

namespace RevitMCPKgCommandSet.Core
{
    public sealed class TraverseStep
    {
        public string EdgeType { get; set; }
        public EdgeDirection Direction { get; set; }
    }

    public static class PathTraversal
    {
        public static HashSet<string> Walk(
            ProjectKg kg,
            string startLlmId,
            IEnumerable<TraverseStep> path)
        {
            var result = new HashSet<string>();
            if (kg == null || string.IsNullOrEmpty(startLlmId)) return result;
            if (!kg.HasNode(startLlmId)) return result;

            var current = new HashSet<string> { startLlmId };

            if (path == null) { result.UnionWith(current); return result; }

            foreach (var step in path)
            {
                if (step == null || string.IsNullOrEmpty(step.EdgeType))
                    return new HashSet<string>();

                var next = new HashSet<string>();
                foreach (var id in current)
                {
                    if (step.Direction == EdgeDirection.Outgoing)
                    {
                        foreach (var e in kg.OutgoingEdges(id, step.EdgeType))
                            next.Add(e.Dst);
                    }
                    else
                    {
                        foreach (var e in kg.IncomingEdges(id, step.EdgeType))
                            next.Add(e.Src);
                    }
                }
                current = next;
                if (current.Count == 0) return current;
            }

            return current;
        }

        // Variable-depth BFS reachability: follow ANY of `edgeTypes` in the
        // given direction(s) up to maxDepth hops, returning every distinct
        // node reached with its BFS depth from the start. The start node is
        // excluded from the result. `direction == null` follows both ways.
        // When includeSoftDeleted is false, tombstoned neighbours are not
        // traversed (cuts F1 cascade); set true for F2 provenance chains
        // (replaced_by) that must walk through deleted predecessors.
        public static List<ReachedNode> Reachable(
            ProjectKg kg,
            string startId,
            HashSet<string> edgeTypes,
            EdgeDirection? direction,
            int maxDepth,
            bool includeSoftDeleted)
        {
            var result = new List<ReachedNode>();
            if (kg == null || string.IsNullOrEmpty(startId) || !kg.HasNode(startId))
                return result;
            if (maxDepth < 1) return result;

            var seen = new HashSet<string> { startId };
            var frontier = new List<string> { startId };

            for (int depth = 1; depth <= maxDepth && frontier.Count > 0; depth++)
            {
                var next = new List<string>();
                foreach (var id in frontier)
                {
                    foreach (var nb in Neighbors(kg, id, edgeTypes, direction))
                    {
                        if (!seen.Add(nb)) continue;
                        if (!kg.HasNode(nb)) continue;
                        var node = kg.GetNode(nb);
                        if (!includeSoftDeleted && node.IsSoftDeleted) continue;
                        result.Add(new ReachedNode { LlmId = nb, Depth = depth });
                        next.Add(nb);
                    }
                }
                frontier = next;
            }
            return result;
        }

        private static IEnumerable<string> Neighbors(
            ProjectKg kg, string id, HashSet<string> edgeTypes, EdgeDirection? direction)
        {
            bool any = edgeTypes == null || edgeTypes.Count == 0;
            if (direction != EdgeDirection.Incoming)
                foreach (var e in kg.OutgoingEdges(id))
                    if (any || edgeTypes.Contains(e.EdgeType)) yield return e.Dst;
            if (direction != EdgeDirection.Outgoing)
                foreach (var e in kg.IncomingEdges(id))
                    if (any || edgeTypes.Contains(e.EdgeType)) yield return e.Src;
        }
    }

    public sealed class ReachedNode
    {
        public string LlmId { get; set; }
        public int Depth { get; set; }
    }
}
