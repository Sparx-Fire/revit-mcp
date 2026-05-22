using System.Collections.Generic;
using System.Linq;
using RevitMCPKgCommandSet.Core;
using Xunit;

namespace RevitMCPKgCommandSet.Tests
{
    public class DriftDetectionTests
    {
        private static (ProjectKg kg, FakeElementReader fake) Seed()
        {
            var kg = new ProjectKg("p1");
            var fake = new FakeElementReader();
            fake.AddLevel(1001, "N0", 0.0);
            fake.AddWallType(2001, "WT200", 0.2);
            fake.AddWall(3001, levelEid: 1001, wallTypeEid: 2001,
                p1: new[] { 0.0, 0.0 }, p2: new[] { 5.0, 0.0 }, length: 5.0, height: 3.0);
            Projection.ApplyAdded(kg, fake, new long[] { 1001, 2001, 3001 });
            return (kg, fake);
        }

        [Fact]
        public void Detect_no_drift_when_kg_matches_revit()
        {
            var (kg, fake) = Seed();

            var report = DriftDetection.Detect(kg, fake);

            Assert.Equal(3, report.TotalChecked);
            Assert.Equal(0, report.DriftCount);
            Assert.Empty(report.Entries);
        }

        [Fact]
        public void Detect_missing_in_kg_when_element_unprojected()
        {
            var (kg, fake) = Seed();
            fake.AddLevel(1002, "N1", 3.0);
            // Don't project 1002 — it stays in the reader, missing from KG.

            var report = DriftDetection.Detect(kg, fake);

            Assert.Equal(4, report.TotalChecked);
            Assert.Equal(1, report.DriftCount);
            var e = report.Entries[0];
            Assert.Equal("missing_in_kg", KindString(e.Kind));
            Assert.Equal(1002, e.RevitId);
            Assert.Equal("Level", e.NodeType);
            Assert.Null(e.LlmId);
        }

        [Fact]
        public void Detect_orphan_kg_node_when_element_removed_from_revit()
        {
            var (kg, fake) = Seed();
            // The wall is in the KG. Remove it from the reader (simulates an
            // out-of-band Revit delete that the projection missed).
            fake.Forget(3001);

            var report = DriftDetection.Detect(kg, fake);

            Assert.Equal(2, report.TotalChecked);
            Assert.Equal(1, report.DriftCount);
            var e = report.Entries[0];
            Assert.Equal("orphan_kg_node", KindString(e.Kind));
            Assert.Equal(3001, e.RevitId);
            Assert.Equal(kg.FindByRevitId(3001), e.LlmId);
        }

        [Fact]
        public void Detect_attrs_diverged_returns_kg_and_revit_values()
        {
            var (kg, fake) = Seed();
            fake.Attrs[1001]["elevation"] = 99.0; // out-of-band param edit

            var report = DriftDetection.Detect(kg, fake);

            Assert.Equal(1, report.DriftCount);
            var e = report.Entries[0];
            Assert.Equal("attrs_diverged", KindString(e.Kind));
            Assert.Equal(1001, e.RevitId);
            Assert.Equal(0.0, e.KgAttrs["elevation"]);
            Assert.Equal(99.0, e.RevitAttrs["elevation"]);
        }

        [Fact]
        public void Detect_tombstoned_but_live_when_kg_soft_deleted_yet_revit_alive()
        {
            var (kg, fake) = Seed();
            var wallLlm = kg.FindByRevitId(3001);
            kg.SoftDelete(wallLlm);

            var report = DriftDetection.Detect(kg, fake);

            Assert.Equal(1, report.DriftCount);
            var e = report.Entries[0];
            Assert.Equal("tombstoned_but_live", KindString(e.Kind));
            Assert.Equal(3001, e.RevitId);
            Assert.Equal(wallLlm, e.LlmId);
        }

        [Fact]
        public void Detect_filters_by_node_type()
        {
            var (kg, fake) = Seed();
            fake.AddLevel(1002, "N1", 3.0); // missing from KG
            fake.Attrs[3001]["height"] = 4.0; // diverged

            var levelsOnly = DriftDetection.Detect(kg, fake, nodeTypeFilter: "Level");

            Assert.Equal(2, levelsOnly.TotalChecked);
            Assert.Equal(1, levelsOnly.DriftCount);
            Assert.Equal("missing_in_kg", KindString(levelsOnly.Entries[0].Kind));
        }

        [Fact]
        public void Detect_soft_deleted_node_absent_from_revit_is_not_orphan()
        {
            // A soft-deleted KG node IS the expected steady state when the
            // Revit element has been deleted. Drift is only an orphan when
            // the live KG node still claims a revit_id that's gone.
            var (kg, fake) = Seed();
            var wallLlm = kg.FindByRevitId(3001);
            kg.SoftDelete(wallLlm);
            fake.Forget(3001);

            var report = DriftDetection.Detect(kg, fake);

            Assert.Equal(0, report.DriftCount);
        }

        [Fact]
        public void Detect_ignores_revit_elements_with_unresolved_node_type()
        {
            var (kg, fake) = Seed();
            fake.Types[7777] = null; // reader says "I don't know what this is"

            var report = DriftDetection.Detect(kg, fake);

            Assert.Equal(3, report.TotalChecked);
            Assert.Equal(0, report.DriftCount);
        }

        private static string KindString(DriftKind k)
        {
            switch (k)
            {
                case DriftKind.MissingInKg: return "missing_in_kg";
                case DriftKind.OrphanKgNode: return "orphan_kg_node";
                case DriftKind.TombstonedButLive: return "tombstoned_but_live";
                case DriftKind.AttrsDiverged: return "attrs_diverged";
            }
            return "?";
        }
    }
}
