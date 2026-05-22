using System;
using System.Collections.Generic;
using System.Linq;

namespace RevitMCPKgCommandSet.Core
{
    public enum DriftKind
    {
        // Element exists in Revit but the KG has no node bound to its
        // revit_id. Either DocumentChanged missed an add, or the element
        // was created before the projection was bootstrapped.
        MissingInKg,

        // A live KG node carries a revit_id, but the Revit document no
        // longer contains an element with that id. Either DocumentChanged
        // missed a delete, or the element was removed out of band.
        OrphanKgNode,

        // KG node is soft-deleted (tombstoned) yet a Revit element with
        // the same revit_id is still alive. P3 resurrection path would
        // recover this on the next ApplyAdded, but until then it's drift.
        TombstonedButLive,

        // Live KG node attrs disagree with what the Revit element reports
        // now (out-of-band parameter edit, file reload, external change).
        AttrsDiverged,
    }

    public sealed class DriftEntry
    {
        public long? RevitId { get; set; }
        public string LlmId { get; set; }
        public string NodeType { get; set; }
        public DriftKind Kind { get; set; }
        public Dictionary<string, object> KgAttrs { get; set; }
        public Dictionary<string, object> RevitAttrs { get; set; }
    }

    public sealed class DriftReport
    {
        public int TotalChecked { get; set; }
        public int DriftCount { get; set; }
        public List<DriftEntry> Entries { get; set; }
    }

    public static class DriftDetection
    {
        public static DriftReport Detect(ProjectKg kg, IElementReader reader, string nodeTypeFilter = null)
        {
            if (kg == null) throw new ArgumentNullException(nameof(kg));
            if (reader == null) throw new ArgumentNullException(nameof(reader));

            var entries = new List<DriftEntry>();
            var seenRevitIds = new HashSet<long>();
            int totalChecked = 0;

            foreach (var eid in reader.EnumerateAllElementIds() ?? Enumerable.Empty<long>())
            {
                var nodeType = reader.ResolveNodeType(eid);
                if (nodeType == null) continue;
                if (nodeTypeFilter != null && nodeType != nodeTypeFilter) continue;
                seenRevitIds.Add(eid);
                totalChecked++;

                var llmId = kg.FindByRevitId(eid);
                if (llmId == null)
                {
                    entries.Add(new DriftEntry
                    {
                        RevitId = eid,
                        NodeType = nodeType,
                        Kind = DriftKind.MissingInKg,
                    });
                    continue;
                }

                var node = kg.GetNode(llmId);
                if (node.IsSoftDeleted)
                {
                    entries.Add(new DriftEntry
                    {
                        RevitId = eid,
                        LlmId = llmId,
                        NodeType = nodeType,
                        Kind = DriftKind.TombstonedButLive,
                    });
                    continue;
                }

                var liveAttrs = reader.ReadAttrs(eid);
                if (liveAttrs == null) continue;

                var divergedKg = new Dictionary<string, object>();
                var divergedRevit = new Dictionary<string, object>();
                foreach (var kv in liveAttrs)
                {
                    if (LifecycleAttrs.Reserved.Contains(kv.Key)) continue;
                    node.Attrs.TryGetValue(kv.Key, out var stored);
                    if (!AttrEquals(stored, kv.Value))
                    {
                        divergedKg[kv.Key] = stored;
                        divergedRevit[kv.Key] = kv.Value;
                    }
                }

                if (divergedRevit.Count > 0)
                {
                    entries.Add(new DriftEntry
                    {
                        RevitId = eid,
                        LlmId = llmId,
                        NodeType = nodeType,
                        Kind = DriftKind.AttrsDiverged,
                        KgAttrs = divergedKg,
                        RevitAttrs = divergedRevit,
                    });
                }
            }

            foreach (var node in kg.Nodes)
            {
                if (node.IsSoftDeleted) continue;
                if (node.RevitId == null) continue;
                if (nodeTypeFilter != null && node.NodeType != nodeTypeFilter) continue;
                var rid = node.RevitId.Value;
                if (seenRevitIds.Contains(rid)) continue;

                entries.Add(new DriftEntry
                {
                    RevitId = rid,
                    LlmId = node.LlmId,
                    NodeType = node.NodeType,
                    Kind = DriftKind.OrphanKgNode,
                });
            }

            return new DriftReport
            {
                TotalChecked = totalChecked,
                DriftCount = entries.Count,
                Entries = entries,
            };
        }

        // Element-wise equality that tolerates the array shapes the reader
        // produces (p1/p2 are double[]). Doubles are compared bit-exact
        // because both sides go through the same reader pipeline; any
        // divergence is a real parameter change, not floating-point noise.
        private static bool AttrEquals(object a, object b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;
            if (Equals(a, b)) return true;

            if (a is Array arrA && b is Array arrB)
            {
                if (arrA.Length != arrB.Length) return false;
                for (int i = 0; i < arrA.Length; i++)
                    if (!AttrEquals(arrA.GetValue(i), arrB.GetValue(i))) return false;
                return true;
            }

            if (a is System.Collections.IEnumerable seqA && b is System.Collections.IEnumerable seqB &&
                !(a is string) && !(b is string))
            {
                var listA = seqA.Cast<object>().ToList();
                var listB = seqB.Cast<object>().ToList();
                if (listA.Count != listB.Count) return false;
                for (int i = 0; i < listA.Count; i++)
                    if (!AttrEquals(listA[i], listB[i])) return false;
                return true;
            }

            return false;
        }
    }
}
