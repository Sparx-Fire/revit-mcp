using System;
using System.Collections.Generic;

namespace RevitMCPKgCommandSet.Core
{
    public sealed class DriftResolutionReport
    {
        public int TotalDetected { get; set; }
        public int TotalResolved { get; set; }
        public int ResolvedMissingInKg { get; set; }
        public int ResolvedAttrsDiverged { get; set; }
        public int ResolvedOrphanKgNode { get; set; }
        public int ResolvedTombstonedButLive { get; set; }
        // Entries that were skipped (kindsFilter excluded them) OR errored
        // during resolution. The Reason field carries the cause when known.
        public List<DriftResolutionSkip> Unresolved { get; set; }
            = new List<DriftResolutionSkip>();
    }

    public sealed class DriftResolutionSkip
    {
        public DriftEntry Entry { get; set; }
        public string Reason { get; set; }
    }

    // Align the KG to the live Revit state on a per-entry basis, preserving
    // history (action_log entries are appended, F2 annotations untouched,
    // bootstrap nodes at created_at_turn=0 stay distinguishable from
    // reconciliation-time additions). The semantic of "resolve" by kind:
    //
    //   MissingInKg       Projection.ApplyAdded([revit_id])   — new node @ current turn
    //   TombstonedButLive Projection.ApplyAdded([revit_id])   — resurrect + attr sync (P3 path)
    //   AttrsDiverged     Projection.ApplyModified([revit_id])— overwrite attrs from reader
    //   OrphanKgNode      kg.SoftDelete(llm_id)               — node hidden from live queries
    //
    // The MissingInKg and TombstonedButLive paths share Projection.ApplyAdded
    // because that method already handles the resurrection branch when the
    // revit_id maps to a soft-deleted node (cf. Projection.cs:38-46).
    public static class DriftResolution
    {
        public static DriftResolutionReport Resolve(
            ProjectKg kg,
            IElementReader reader,
            DriftReport drift,
            ISet<DriftKind> kindsFilter = null)
        {
            if (kg == null) throw new ArgumentNullException(nameof(kg));
            if (reader == null) throw new ArgumentNullException(nameof(reader));
            if (drift == null) throw new ArgumentNullException(nameof(drift));

            var report = new DriftResolutionReport
            {
                TotalDetected = drift.DriftCount,
            };

            if (drift.Entries == null) return report;

            foreach (var entry in drift.Entries)
            {
                if (kindsFilter != null && !kindsFilter.Contains(entry.Kind))
                {
                    report.Unresolved.Add(new DriftResolutionSkip
                    {
                        Entry = entry,
                        Reason = "filtered_by_kinds",
                    });
                    continue;
                }

                try
                {
                    ApplyOne(kg, reader, entry, report);
                }
                catch (Exception ex)
                {
                    report.Unresolved.Add(new DriftResolutionSkip
                    {
                        Entry = entry,
                        Reason = ex.GetType().Name + ": " + ex.Message,
                    });
                }
            }

            report.TotalResolved =
                report.ResolvedMissingInKg
                + report.ResolvedAttrsDiverged
                + report.ResolvedOrphanKgNode
                + report.ResolvedTombstonedButLive;

            return report;
        }

        private static void ApplyOne(
            ProjectKg kg, IElementReader reader,
            DriftEntry entry, DriftResolutionReport report)
        {
            switch (entry.Kind)
            {
                case DriftKind.MissingInKg:
                case DriftKind.TombstonedButLive:
                {
                    if (entry.RevitId == null)
                    {
                        report.Unresolved.Add(new DriftResolutionSkip
                        { Entry = entry, Reason = "no_revit_id" });
                        return;
                    }
                    var stats = Projection.ApplyAdded(
                        kg, reader, new[] { entry.RevitId.Value });
                    if (stats.NodesAffected > 0)
                    {
                        if (entry.Kind == DriftKind.MissingInKg)
                            report.ResolvedMissingInKg++;
                        else
                            report.ResolvedTombstonedButLive++;
                    }
                    else
                    {
                        report.Unresolved.Add(new DriftResolutionSkip
                        { Entry = entry, Reason = "ApplyAdded_skipped" });
                    }
                    break;
                }

                case DriftKind.AttrsDiverged:
                {
                    if (entry.RevitId == null)
                    {
                        report.Unresolved.Add(new DriftResolutionSkip
                        { Entry = entry, Reason = "no_revit_id" });
                        return;
                    }
                    var stats = Projection.ApplyModified(
                        kg, reader, new[] { entry.RevitId.Value });
                    if (stats.NodesAffected > 0)
                        report.ResolvedAttrsDiverged++;
                    else
                        report.Unresolved.Add(new DriftResolutionSkip
                        { Entry = entry, Reason = "ApplyModified_skipped" });
                    break;
                }

                case DriftKind.OrphanKgNode:
                {
                    if (string.IsNullOrEmpty(entry.LlmId))
                    {
                        report.Unresolved.Add(new DriftResolutionSkip
                        { Entry = entry, Reason = "no_llm_id" });
                        return;
                    }
                    kg.SoftDelete(entry.LlmId);
                    report.ResolvedOrphanKgNode++;
                    break;
                }

                default:
                    report.Unresolved.Add(new DriftResolutionSkip
                    { Entry = entry, Reason = "unknown_kind" });
                    break;
            }
        }
    }
}
