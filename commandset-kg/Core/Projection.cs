using System;
using System.Collections.Generic;
using System.Linq;

namespace RevitMCPKgCommandSet.Core
{
    public readonly struct ProjectionStats
    {
        public int NodesAffected { get; }
        public int EdgesAffected { get; }
        public int Skipped { get; }

        public ProjectionStats(int nodesAffected, int edgesAffected, int skipped)
        {
            NodesAffected = nodesAffected;
            EdgesAffected = edgesAffected;
            Skipped = skipped;
        }
    }

    public static class Projection
    {
        public static ProjectionStats ApplyAdded(ProjectKg kg, IElementReader reader, IEnumerable<long> addedElementIds)
        {
            if (kg == null) throw new ArgumentNullException(nameof(kg));
            if (reader == null) throw new ArgumentNullException(nameof(reader));
            if (addedElementIds == null) return new ProjectionStats(0, 0, 0);

            var newNodes = new Dictionary<long, string>();
            var resurrected = new List<long>();
            int skipped = 0;

            foreach (var eid in addedElementIds)
            {
                // Ctrl+Z on a delete: Revit re-uses the original ElementId.
                // Preserve the llm_id by resurrecting the tombstone instead
                // of forging a duplicate node bound to the same revit_id.
                var existingLlmId = kg.FindByRevitId(eid);
                if (existingLlmId != null)
                {
                    var existingNode = kg.GetNode(existingLlmId);
                    if (existingNode.IsSoftDeleted)
                    {
                        kg.Resurrect(existingLlmId);
                        resurrected.Add(eid);
                    }
                    else
                    {
                        // A live node already owns this revit_id. Revit
                        // doesn't reuse live ElementIds, so this is a
                        // defensive guard, not an expected branch.
                        skipped++;
                    }
                    continue;
                }

                var nodeType = reader.ResolveNodeType(eid);
                if (nodeType == null) { skipped++; continue; }
                var attrs = reader.ReadAttrs(eid);
                if (attrs == null) { skipped++; continue; }

                try
                {
                    var llmId = kg.AddNode(nodeType, attrs);
                    kg.SetRevitId(llmId, eid);
                    newNodes[eid] = llmId;
                }
                catch (ArgumentException)
                {
                    skipped++;
                }
            }

            int edgesCreated = 0;
            foreach (var pair in newNodes)
            {
                var thisLlmId = pair.Value;
                foreach (var edge in reader.ReadEdges(pair.Key) ?? Enumerable.Empty<EdgeSpec>())
                {
                    var peerLlmId = kg.FindByRevitId(edge.PeerElementId);
                    if (peerLlmId == null) continue;
                    var src = edge.Direction == EdgeDirection.Outgoing ? thisLlmId : peerLlmId;
                    var dst = edge.Direction == EdgeDirection.Outgoing ? peerLlmId : thisLlmId;
                    try
                    {
                        if (kg.AddEdge(src, dst, edge.EdgeType)) edgesCreated++;
                    }
                    catch (ArgumentException) { }
                }
            }

            // Resurrected nodes need attr/edge sync to match the current
            // Revit state (the tombstoned snapshot may be stale).
            int resurrectedEdges = 0;
            if (resurrected.Count > 0)
            {
                var modStats = ApplyModified(kg, reader, resurrected);
                resurrectedEdges = modStats.EdgesAffected;
            }

            return new ProjectionStats(
                newNodes.Count + resurrected.Count,
                edgesCreated + resurrectedEdges,
                skipped);
        }

        public static ProjectionStats ApplyDeleted(ProjectKg kg, IEnumerable<long> deletedElementIds)
        {
            if (kg == null) throw new ArgumentNullException(nameof(kg));
            if (deletedElementIds == null) return new ProjectionStats(0, 0, 0);

            int deleted = 0;
            int skipped = 0;
            foreach (var eid in deletedElementIds)
            {
                var llmId = kg.FindByRevitId(eid);
                if (llmId == null) { skipped++; continue; }
                kg.SoftDelete(llmId);
                deleted++;
            }
            return new ProjectionStats(deleted, 0, skipped);
        }

        public static ProjectionStats ApplyModified(ProjectKg kg, IElementReader reader, IEnumerable<long> modifiedElementIds)
        {
            if (kg == null) throw new ArgumentNullException(nameof(kg));
            if (reader == null) throw new ArgumentNullException(nameof(reader));
            if (modifiedElementIds == null) return new ProjectionStats(0, 0, 0);

            int nodesUpdated = 0;
            int edgesPatched = 0;
            int skipped = 0;

            foreach (var eid in modifiedElementIds)
            {
                var llmId = kg.FindByRevitId(eid);
                if (llmId == null) { skipped++; continue; }

                var nodeType = reader.ResolveNodeType(eid);
                if (nodeType == null) { skipped++; continue; }

                var newAttrs = reader.ReadAttrs(eid);
                if (newAttrs == null) { skipped++; continue; }

                var node = kg.GetNode(llmId);
                if (node.IsSoftDeleted) { skipped++; continue; }

                var updates = new Dictionary<string, object>();
                foreach (var kvp in newAttrs)
                {
                    if (LifecycleAttrs.Reserved.Contains(kvp.Key)) continue;
                    if (!node.Attrs.TryGetValue(kvp.Key, out var oldVal) || !Equals(oldVal, kvp.Value))
                        updates[kvp.Key] = kvp.Value;
                }

                if (updates.Count > 0)
                {
                    try
                    {
                        kg.ModifyNode(llmId, updates);
                        nodesUpdated++;
                    }
                    catch (ArgumentException) { skipped++; continue; }
                }

                var oldF1Edges = kg.OutgoingEdges(llmId)
                    .Concat(kg.IncomingEdges(llmId))
                    .Where(e => EdgeTypes.F1.Contains(e.EdgeType))
                    .Distinct()
                    .ToList();
                foreach (var e in oldF1Edges) kg.RemoveEdge(e.Src, e.Dst, e.EdgeType);

                foreach (var edge in reader.ReadEdges(eid) ?? Enumerable.Empty<EdgeSpec>())
                {
                    var peerLlmId = kg.FindByRevitId(edge.PeerElementId);
                    if (peerLlmId == null) continue;
                    var src = edge.Direction == EdgeDirection.Outgoing ? llmId : peerLlmId;
                    var dst = edge.Direction == EdgeDirection.Outgoing ? peerLlmId : llmId;
                    try
                    {
                        if (kg.AddEdge(src, dst, edge.EdgeType)) edgesPatched++;
                    }
                    catch (ArgumentException) { }
                }
            }

            return new ProjectionStats(nodesUpdated, edgesPatched, skipped);
        }
    }
}
