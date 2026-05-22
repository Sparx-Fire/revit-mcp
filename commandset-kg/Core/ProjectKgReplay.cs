using System;
using System.Collections.Generic;

namespace RevitMCPKgCommandSet.Core
{
    public static class ProjectKgReplay
    {
        public sealed class ReplayStats
        {
            public int Applied { get; set; }
            public int Skipped { get; set; }
            public List<string> SkipReasons { get; } = new List<string>();
        }

        public static (ProjectKg kg, ReplayStats stats) Replay(string projectId, IEnumerable<DeltaEntry> entries)
        {
            var kg = new ProjectKg(projectId);
            var stats = new ReplayStats();
            if (entries == null) return (kg, stats);

            foreach (var e in entries)
            {
                if (e == null || string.IsNullOrEmpty(e.Op))
                {
                    stats.Skipped++;
                    continue;
                }
                try
                {
                    Apply(kg, e);
                    stats.Applied++;
                }
                catch (Exception ex)
                {
                    stats.Skipped++;
                    stats.SkipReasons.Add($"{e.Op} {e.Id ?? e.Src}: {ex.Message}");
                }
            }

            return (kg, stats);
        }

        private static void Apply(ProjectKg kg, DeltaEntry e)
        {
            switch (e.Op)
            {
                case DeltaOps.AdvanceTurn:
                    kg.AdvanceTurn();
                    break;
                case DeltaOps.CreateNode:
                    kg.AddNode(e.NodeType, e.Attrs ?? new Dictionary<string, object>(), llmId: e.Id);
                    break;
                case DeltaOps.CreateUserNode:
                    kg.AddUserNode(e.NodeType, e.Attrs ?? new Dictionary<string, object>(), llmId: e.Id);
                    break;
                case DeltaOps.ModifyNode:
                    kg.ModifyNode(e.Id, e.Updates ?? new Dictionary<string, object>());
                    break;
                case DeltaOps.SoftDelete:
                    kg.SoftDelete(e.Id);
                    break;
                case DeltaOps.Resurrect:
                    kg.Resurrect(e.Id);
                    break;
                case DeltaOps.SetRevitId:
                    if (e.RevitId.HasValue) kg.SetRevitId(e.Id, e.RevitId.Value);
                    break;
                case DeltaOps.AddEdge:
                    kg.AddEdge(e.Src, e.Dst, e.EdgeType, e.Attrs);
                    break;
                case DeltaOps.RemoveEdge:
                    kg.RemoveEdge(e.Src, e.Dst, e.EdgeType);
                    break;
                case DeltaOps.Annotate:
                    // payload absent on the wire (NullValueHandling.Ignore)
                    // is the delete sentinel for F2 annotations.
                    kg.Annotate(e.Src, e.Dst, e.EdgeType, e.Attrs);
                    break;
                default:
                    throw new NotSupportedException($"unknown op: {e.Op}");
            }
        }
    }
}
