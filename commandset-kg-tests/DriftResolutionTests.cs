using System.Collections.Generic;
using System.Linq;
using RevitMCPKgCommandSet.Core;
using Xunit;

namespace RevitMCPKgCommandSet.Tests
{
    public class DriftResolutionTests
    {
        private static (ProjectKg kg, FakeElementReader fake) Seed()
        {
            var kg = new ProjectKg("p1");
            var fake = new FakeElementReader();
            fake.AddLevel(1001, "N0", 0.0);
            fake.AddWallType(2001, "WT200", 0.2);
            fake.AddWall(3001, levelEid: 1001, wallTypeEid: 2001,
                p1: new[] { 0.0, 0.0 }, p2: new[] { 5.0, 0.0 },
                length: 5.0, height: 3.0);
            Projection.ApplyAdded(kg, fake, new long[] { 1001, 2001, 3001 });
            return (kg, fake);
        }

        private static DriftResolutionReport DetectAndResolve(
            ProjectKg kg, FakeElementReader fake,
            ISet<DriftKind> kinds = null)
        {
            var drift = DriftDetection.Detect(kg, fake);
            return DriftResolution.Resolve(kg, fake, drift, kinds);
        }

        [Fact]
        public void Resolve_no_drift_is_noop()
        {
            var (kg, fake) = Seed();

            var r = DetectAndResolve(kg, fake);

            Assert.Equal(0, r.TotalDetected);
            Assert.Equal(0, r.TotalResolved);
            Assert.Empty(r.Unresolved);
        }

        [Fact]
        public void Resolve_missing_in_kg_creates_node_and_resyncs_edges()
        {
            var (kg, fake) = Seed();
            fake.AddLevel(1002, "N1", 3.0);
            // Don't project 1002 — drift = missing_in_kg.

            int countBefore = kg.NodeCount;
            var r = DetectAndResolve(kg, fake);

            Assert.Equal(1, r.TotalDetected);
            Assert.Equal(1, r.ResolvedMissingInKg);
            Assert.Equal(1, r.TotalResolved);
            Assert.Empty(r.Unresolved);
            Assert.Equal(countBefore + 1, kg.NodeCount);
            Assert.NotNull(kg.FindByRevitId(1002));
        }

        [Fact]
        public void Resolve_attrs_diverged_overwrites_kg_attrs_from_revit()
        {
            var (kg, fake) = Seed();
            fake.Attrs[1001]["elevation"] = 99.0; // out-of-band edit

            var r = DetectAndResolve(kg, fake);

            Assert.Equal(1, r.TotalDetected);
            Assert.Equal(1, r.ResolvedAttrsDiverged);
            // KG should now match Revit.
            var llmId = kg.FindByRevitId(1001);
            Assert.Equal(99.0, kg.GetNode(llmId).Attrs["elevation"]);
            // Re-detect must find no drift.
            Assert.Equal(0, DriftDetection.Detect(kg, fake).DriftCount);
        }

        [Fact]
        public void Resolve_orphan_kg_node_soft_deletes()
        {
            var (kg, fake) = Seed();
            fake.Forget(3001); // out-of-band Revit delete

            var r = DetectAndResolve(kg, fake);

            Assert.Equal(1, r.TotalDetected);
            Assert.Equal(1, r.ResolvedOrphanKgNode);
            var llmId = kg.FindByRevitId(3001);
            Assert.True(kg.GetNode(llmId).IsSoftDeleted);
            // After soft-delete, the orphan is no longer drift.
            Assert.Equal(0, DriftDetection.Detect(kg, fake).DriftCount);
        }

        [Fact]
        public void Resolve_tombstoned_but_live_resurrects_and_syncs_attrs()
        {
            var (kg, fake) = Seed();
            var wallLlm = kg.FindByRevitId(3001);
            kg.SoftDelete(wallLlm);
            // Now the wall is tombstoned but Revit still has it AND we'll
            // pretend its height changed in Revit between the soft-delete
            // and the resurrection.
            fake.Attrs[3001]["height"] = 4.2;

            var r = DetectAndResolve(kg, fake);

            Assert.Equal(1, r.TotalDetected);
            Assert.Equal(1, r.ResolvedTombstonedButLive);
            Assert.False(kg.GetNode(wallLlm).IsSoftDeleted);
            // ApplyAdded internally calls ApplyModified on resurrection →
            // attrs converge to Revit (h=4.2, not the tombstoned h=3.0).
            Assert.Equal(4.2, kg.GetNode(wallLlm).Attrs["height"]);
        }

        [Fact]
        public void Resolve_filters_by_kinds()
        {
            var (kg, fake) = Seed();
            fake.AddLevel(1002, "N1", 3.0);     // missing_in_kg
            fake.Attrs[1001]["elevation"] = 9.9; // attrs_diverged

            var r = DriftResolution.Resolve(
                kg, fake, DriftDetection.Detect(kg, fake),
                kindsFilter: new HashSet<DriftKind> { DriftKind.MissingInKg });

            Assert.Equal(2, r.TotalDetected);
            Assert.Equal(1, r.ResolvedMissingInKg);
            Assert.Equal(0, r.ResolvedAttrsDiverged);
            // The attrs_diverged entry is in Unresolved with reason.
            Assert.Single(r.Unresolved);
            Assert.Equal("filtered_by_kinds", r.Unresolved[0].Reason);
            Assert.Equal(DriftKind.AttrsDiverged, r.Unresolved[0].Entry.Kind);
        }

        [Fact]
        public void Resolve_idempotent_when_run_twice()
        {
            var (kg, fake) = Seed();
            fake.AddLevel(1002, "N1", 3.0);
            fake.Attrs[1001]["elevation"] = 9.9;
            fake.Forget(3001);

            var r1 = DetectAndResolve(kg, fake);
            Assert.True(r1.TotalResolved >= 3);

            // Second pass: drift should be 0 → 0 resolutions.
            var r2 = DetectAndResolve(kg, fake);
            Assert.Equal(0, r2.TotalDetected);
            Assert.Equal(0, r2.TotalResolved);
        }

        [Fact]
        public void Resolve_preserves_action_log_entries()
        {
            var (kg, fake) = Seed();
            int logBefore = kg.ActionLog.Count;
            fake.AddLevel(1002, "N1", 3.0);
            fake.Attrs[1001]["elevation"] = 9.9;

            DetectAndResolve(kg, fake);

            // Resolution must APPEND to action_log, never truncate. New
            // entries: create (missing_in_kg), modify (attrs_diverged).
            Assert.True(kg.ActionLog.Count >= logBefore + 2);
            // Original entries are still there at their original indices.
            Assert.True(kg.ActionLog.Count > logBefore);
        }

        [Fact]
        public void Resolve_preserves_f2_annotations()
        {
            var (kg, fake) = Seed();
            // Add an F2 annotation between two existing nodes BEFORE drift
            // resolution. Resolution must not touch it.
            var levelLlm = kg.FindByRevitId(1001);
            var wallLlm = kg.FindByRevitId(3001);
            kg.Annotate(wallLlm, levelLlm, "tagged",
                new Dictionary<string, object> { ["note"] = "structural ref" });

            fake.AddLevel(1002, "N1", 3.0);
            fake.Attrs[1001]["elevation"] = 9.9;
            DetectAndResolve(kg, fake);

            // The F2 edge must still exist post-resolve.
            var edges = kg.OutgoingEdges(wallLlm)
                .Where(e => e.EdgeType == "tagged")
                .ToList();
            Assert.Single(edges);
            Assert.Equal(levelLlm, edges[0].Dst);
        }

        [Fact]
        public void Resolve_unresolved_when_revit_id_null()
        {
            var kg = new ProjectKg("p1");
            var fake = new FakeElementReader();
            // Hand-craft a DriftEntry with no revit_id (shouldn't happen via
            // Detect, but Resolve must be defensive).
            var drift = new DriftReport
            {
                TotalChecked = 0,
                DriftCount = 1,
                Entries = new List<DriftEntry>
                {
                    new DriftEntry
                    {
                        Kind = DriftKind.MissingInKg,
                        NodeType = "Wall",
                        RevitId = null,
                    }
                }
            };

            var r = DriftResolution.Resolve(kg, fake, drift);

            Assert.Equal(0, r.TotalResolved);
            Assert.Single(r.Unresolved);
            Assert.Equal("no_revit_id", r.Unresolved[0].Reason);
        }

        [Fact]
        public void Resolve_filters_orphan_by_kinds()
        {
            var (kg, fake) = Seed();
            fake.Forget(3001);

            var r = DriftResolution.Resolve(
                kg, fake, DriftDetection.Detect(kg, fake),
                kindsFilter: new HashSet<DriftKind> { DriftKind.AttrsDiverged });

            Assert.Equal(1, r.TotalDetected);
            Assert.Equal(0, r.ResolvedOrphanKgNode);
            Assert.Single(r.Unresolved);
            Assert.Equal("filtered_by_kinds", r.Unresolved[0].Reason);
            // Wall stays live in KG (not soft-deleted).
            var wallLlm = kg.FindByRevitId(3001);
            Assert.False(kg.GetNode(wallLlm).IsSoftDeleted);
        }
    }
}
