using System;
using System.Collections.Generic;
using RevitMCPKgCommandSet.Core;
using Xunit;

namespace RevitMCPKgCommandSet.Tests
{
    public class ProjectKgNodeTests
    {
        private static Dictionary<string, object> LevelAttrs(double elevation = 0.0, string name = "N0") =>
            new() { ["name"] = name, ["elevation"] = elevation };

        private static Dictionary<string, object> WallAttrs(string lvl, string wt) =>
            new()
            {
                ["type_ref"] = wt,
                ["level_ref"] = lvl,
                ["p1"] = new[] { 0.0, 0.0 },
                ["p2"] = new[] { 5.0, 0.0 },
                ["length"] = 5.0,
                ["height"] = 3.0,
            };

        [Fact]
        public void New_kg_starts_empty()
        {
            var kg = new ProjectKg("p1");
            Assert.Equal("p1", kg.ProjectId);
            Assert.Equal(0, kg.Turn);
            Assert.Equal(0, kg.NodeCount);
            Assert.Equal(0, kg.EdgeCount);
        }

        [Fact]
        public void AddNode_with_explicit_llmId_bumps_counter_for_next_auto_id()
        {
            // Replay scenario: a journal restored level_001/level_002/level_003
            // via AddNode(llmId: ...). The next auto-id call must not collide
            // — it must produce level_004 (not loop back to level_001).
            var kg = new ProjectKg("p1");
            kg.AddNode("Level", LevelAttrs(0.0, "L1"), llmId: "level_001");
            kg.AddNode("Level", LevelAttrs(3.0, "L2"), llmId: "level_002");
            kg.AddNode("Level", LevelAttrs(6.0, "L3"), llmId: "level_003");

            var autoId = kg.AddNode("Level", LevelAttrs(9.0, "L4"));

            Assert.Equal("level_004", autoId);
            Assert.Equal(4, kg.NodeCount);
        }

        [Fact]
        public void AddNode_explicit_llmId_does_not_lower_counter()
        {
            // If a caller supplies a LOWER id than current counter (manual
            // restore), counter must not regress — NextLlmId stays monotonic.
            var kg = new ProjectKg("p1");
            kg.AddNode("Level", LevelAttrs(0.0, "L1"));  // auto: level_001
            kg.AddNode("Level", LevelAttrs(3.0, "L2"));  // auto: level_002
            // Out-of-band insert with an explicit, much lower-looking id.
            kg.AddNode("Level", LevelAttrs(6.0, "L0"), llmId: "level_009");
            // Next auto must be level_010, not level_003.
            var autoId = kg.AddNode("Level", LevelAttrs(12.0, "L_top"));
            Assert.Equal("level_010", autoId);
        }

        [Fact]
        public void AddNode_freeform_llmId_does_not_bump_counter()
        {
            // Non-conformant ids (no "<type>_<NNN>" pattern) are accepted
            // but don't move the counter — NextLlmId starts at 001.
            var kg = new ProjectKg("p1");
            kg.AddNode("Level", LevelAttrs(0.0, "L0"), llmId: "level_custom");
            var autoId = kg.AddNode("Level", LevelAttrs(3.0, "L1"));
            Assert.Equal("level_001", autoId);
        }

        [Fact]
        public void AdvanceTurn_increments_monotonically()
        {
            var kg = new ProjectKg("p1");
            Assert.Equal(1, kg.AdvanceTurn());
            Assert.Equal(2, kg.AdvanceTurn());
            Assert.Equal(2, kg.Turn);
        }

        [Fact]
        public void AddNode_allocates_typed_llm_id()
        {
            var kg = new ProjectKg("p1");
            var id = kg.AddNode("Level", LevelAttrs());
            Assert.Equal("level_001", id);
        }

        [Fact]
        public void AddNode_increments_per_type_counter()
        {
            var kg = new ProjectKg("p1");
            kg.AddNode("Level", LevelAttrs(0, "N0"));
            kg.AddNode("Level", LevelAttrs(3, "N1"));
            var third = kg.AddNode("Level", LevelAttrs(6, "N2"));
            Assert.Equal("level_003", third);
        }

        [Fact]
        public void AddNode_counters_are_per_type()
        {
            var kg = new ProjectKg("p1");
            kg.AddNode("WallType", new() { ["name"] = "WT200", ["total_thickness"] = 0.2 });
            kg.AddNode("Level", LevelAttrs());
            var wt2 = kg.AddNode("WallType", new() { ["name"] = "WT300", ["total_thickness"] = 0.3 });
            Assert.Equal("walltype_002", wt2);
        }

        [Fact]
        public void AddNode_rejects_unknown_type()
        {
            var kg = new ProjectKg("p1");
            Assert.Throws<ArgumentException>(() =>
                kg.AddNode("FooBar", new() { ["x"] = 1 }));
        }

        [Fact]
        public void AddNode_rejects_missing_required()
        {
            var kg = new ProjectKg("p1");
            var ex = Assert.Throws<ArgumentException>(() =>
                kg.AddNode("Level", new() { ["name"] = "N0" }));
            Assert.Contains("elevation", ex.Message);
        }

        [Fact]
        public void AddNode_rejects_unknown_attr()
        {
            var kg = new ProjectKg("p1");
            var ex = Assert.Throws<ArgumentException>(() =>
                kg.AddNode("Level", new() { ["name"] = "N0", ["elevation"] = 0.0, ["bogus"] = true }));
            Assert.Contains("bogus", ex.Message);
        }

        [Fact]
        public void AddNode_explicit_llm_id_is_used()
        {
            var kg = new ProjectKg("p1");
            var id = kg.AddNode("Level", LevelAttrs(), llmId: "ground");
            Assert.Equal("ground", id);
            Assert.True(kg.HasNode("ground"));
        }

        [Fact]
        public void AddNode_rejects_duplicate_llm_id()
        {
            var kg = new ProjectKg("p1");
            kg.AddNode("Level", LevelAttrs(), llmId: "ground");
            Assert.Throws<ArgumentException>(() =>
                kg.AddNode("Level", LevelAttrs(3, "N1"), llmId: "ground"));
        }

        [Fact]
        public void AddNode_stamps_lifecycle_attrs()
        {
            var kg = new ProjectKg("p1");
            kg.AdvanceTurn();
            kg.AdvanceTurn();
            var id = kg.AddNode("Level", LevelAttrs());
            var node = kg.GetNode(id);
            Assert.Equal("Level", node.NodeType);
            Assert.Equal(2, node.CreatedAtTurn);
            Assert.Empty(node.ModifiedAtTurns);
            Assert.Null(node.DeletedAtTurn);
            Assert.False(node.IsSoftDeleted);
        }

        [Fact]
        public void ModifyNode_updates_attr()
        {
            var kg = new ProjectKg("p1");
            var id = kg.AddNode("Level", LevelAttrs());
            kg.AdvanceTurn();
            kg.ModifyNode(id, new() { ["elevation"] = 2.5 });
            Assert.Equal(2.5, kg.GetNode(id).Attrs["elevation"]);
        }

        [Fact]
        public void ModifyNode_appends_modified_at_turn()
        {
            var kg = new ProjectKg("p1");
            var id = kg.AddNode("Level", LevelAttrs());
            kg.AdvanceTurn();
            kg.ModifyNode(id, new() { ["elevation"] = 1.0 });
            kg.AdvanceTurn();
            kg.ModifyNode(id, new() { ["elevation"] = 2.0 });
            Assert.Equal(new[] { 1, 2 }, kg.GetNode(id).ModifiedAtTurns);
        }

        [Fact]
        public void ModifyNode_rejects_unknown_attr()
        {
            var kg = new ProjectKg("p1");
            var id = kg.AddNode("Level", LevelAttrs());
            Assert.Throws<ArgumentException>(() =>
                kg.ModifyNode(id, new() { ["bogus"] = 1 }));
        }

        [Fact]
        public void ModifyNode_rejects_soft_deleted()
        {
            var kg = new ProjectKg("p1");
            var id = kg.AddNode("Level", LevelAttrs());
            kg.SoftDelete(id);
            Assert.Throws<InvalidOperationException>(() =>
                kg.ModifyNode(id, new() { ["elevation"] = 1.0 }));
        }

        [Fact]
        public void ModifyNode_rejects_unknown_id()
        {
            var kg = new ProjectKg("p1");
            Assert.Throws<KeyNotFoundException>(() =>
                kg.ModifyNode("does_not_exist", new() { ["x"] = 1 }));
        }

        [Fact]
        public void SoftDelete_marks_deleted_at_turn()
        {
            var kg = new ProjectKg("p1");
            var id = kg.AddNode("Level", LevelAttrs());
            kg.AdvanceTurn();
            kg.SoftDelete(id);
            Assert.Equal(1, kg.GetNode(id).DeletedAtTurn);
            Assert.True(kg.GetNode(id).IsSoftDeleted);
        }

        [Fact]
        public void SoftDelete_is_idempotent()
        {
            var kg = new ProjectKg("p1");
            var id = kg.AddNode("Level", LevelAttrs());
            kg.AdvanceTurn();
            kg.SoftDelete(id);
            var firstStamp = kg.GetNode(id).DeletedAtTurn;
            kg.AdvanceTurn();
            kg.SoftDelete(id);
            Assert.Equal(firstStamp, kg.GetNode(id).DeletedAtTurn);
        }

        [Fact]
        public void SetRevitId_then_FindByRevitId_roundtrips()
        {
            var kg = new ProjectKg("p1");
            var id = kg.AddNode("Wall", WallAttrs("level_001", "walltype_001"), llmId: "wall_001");
            kg.SetRevitId(id, 12345L);
            Assert.Equal(12345L, kg.GetNode(id).RevitId);
            Assert.Equal("wall_001", kg.FindByRevitId(12345L));
        }

        [Fact]
        public void FindByRevitId_returns_null_for_unbound()
        {
            var kg = new ProjectKg("p1");
            Assert.Null(kg.FindByRevitId(99999L));
        }

        [Fact]
        public void NodesOfType_filters_correctly()
        {
            var kg = new ProjectKg("p1");
            kg.AddNode("Level", LevelAttrs(0, "N0"));
            kg.AddNode("Level", LevelAttrs(3, "N1"));
            kg.AddNode("WallType", new() { ["name"] = "WT200", ["total_thickness"] = 0.2 });
            Assert.Equal(2, System.Linq.Enumerable.Count(kg.NodesOfType("Level")));
            Assert.Single(kg.NodesOfType("WallType"));
        }
    }
}
