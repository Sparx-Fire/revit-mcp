using System;
using System.Collections.Generic;
using RevitMCPKgCommandSet.Core;
using Xunit;

namespace RevitMCPKgCommandSet.Tests
{
    public class ProjectKgTransactionTests
    {
        private static Dictionary<string, object> LevelAttrs(string name = "N0", double elev = 0.0) =>
            new() { ["name"] = name, ["elevation"] = elev };

        [Fact]
        public void Transaction_committed_persists_node()
        {
            var kg = new ProjectKg("p1");
            using (var tx = kg.BeginTransaction())
            {
                kg.AddNode("Level", LevelAttrs());
                tx.Commit();
            }
            Assert.Equal(1, kg.NodeCount);
        }

        [Fact]
        public void Transaction_not_committed_rolls_back_node()
        {
            var kg = new ProjectKg("p1");
            using (var tx = kg.BeginTransaction())
            {
                kg.AddNode("Level", LevelAttrs());
                // no Commit
            }
            Assert.Equal(0, kg.NodeCount);
        }

        [Fact]
        public void Transaction_rolls_back_on_exception()
        {
            var kg = new ProjectKg("p1");
            try
            {
                using var tx = kg.BeginTransaction();
                kg.AddNode("Level", LevelAttrs());
                throw new InvalidOperationException("oops");
            }
            catch (InvalidOperationException) { /* expected */ }
            Assert.Equal(0, kg.NodeCount);
        }

        [Fact]
        public void Transaction_rollback_restores_turn()
        {
            var kg = new ProjectKg("p1");
            kg.AdvanceTurn();
            Assert.Equal(1, kg.Turn);
            using (var tx = kg.BeginTransaction())
            {
                kg.AdvanceTurn();
                kg.AdvanceTurn();
                Assert.Equal(3, kg.Turn);
            }
            Assert.Equal(1, kg.Turn);
        }

        [Fact]
        public void Transaction_rollback_restores_counters()
        {
            var kg = new ProjectKg("p1");
            kg.AddNode("Level", LevelAttrs("N0"));
            using (var tx = kg.BeginTransaction())
            {
                kg.AddNode("Level", LevelAttrs("N1", 3.0));
            }
            var nextLevel = kg.AddNode("Level", LevelAttrs("N1bis", 3.0));
            Assert.Equal("level_002", nextLevel);
        }

        [Fact]
        public void Transaction_rolls_back_edges_too()
        {
            var kg = new ProjectKg("p1");
            kg.AddNode("Level", LevelAttrs(), llmId: "level_001");
            kg.AddNode("WallType", new() { ["name"] = "WT200", ["total_thickness"] = 0.2 }, llmId: "walltype_001");
            kg.AddNode("Wall", new()
            {
                ["type_ref"] = "walltype_001",
                ["level_ref"] = "level_001",
                ["p1"] = new[] { 0.0, 0.0 },
                ["p2"] = new[] { 5.0, 0.0 },
                ["length"] = 5.0,
                ["height"] = 3.0,
            }, llmId: "wall_001");

            using (var tx = kg.BeginTransaction())
            {
                kg.AddEdge("wall_001", "level_001", EdgeTypes.AtLevel);
                Assert.Equal(1, kg.EdgeCount);
            }

            Assert.Equal(0, kg.EdgeCount);
        }

        [Fact]
        public void Transaction_rollback_restores_modified_attrs()
        {
            var kg = new ProjectKg("p1");
            kg.AddNode("Level", LevelAttrs(elev: 0.0), llmId: "lvl");
            using (var tx = kg.BeginTransaction())
            {
                kg.ModifyNode("lvl", new() { ["elevation"] = 9.9 });
                Assert.Equal(9.9, kg.GetNode("lvl").Attrs["elevation"]);
            }
            Assert.Equal(0.0, kg.GetNode("lvl").Attrs["elevation"]);
        }

        [Fact]
        public void Transaction_rollback_unsoft_deletes()
        {
            var kg = new ProjectKg("p1");
            kg.AddNode("Level", LevelAttrs(), llmId: "lvl");
            using (var tx = kg.BeginTransaction())
            {
                kg.SoftDelete("lvl");
                Assert.True(kg.GetNode("lvl").IsSoftDeleted);
            }
            Assert.False(kg.GetNode("lvl").IsSoftDeleted);
        }

        [Fact]
        public void Transaction_committed_persists_modifications()
        {
            var kg = new ProjectKg("p1");
            kg.AddNode("Level", LevelAttrs(elev: 0.0), llmId: "lvl");
            using (var tx = kg.BeginTransaction())
            {
                kg.AdvanceTurn();
                kg.ModifyNode("lvl", new() { ["elevation"] = 9.9 });
                tx.Commit();
            }
            Assert.Equal(9.9, kg.GetNode("lvl").Attrs["elevation"]);
            Assert.Single(kg.GetNode("lvl").ModifiedAtTurns);
        }

        [Fact]
        public void Action_log_grows_then_rolls_back()
        {
            var kg = new ProjectKg("p1");
            kg.AddNode("Level", LevelAttrs(), llmId: "lvl");
            Assert.Single(kg.ActionLog);

            using (var tx = kg.BeginTransaction())
            {
                kg.AdvanceTurn();
                kg.ModifyNode("lvl", new() { ["elevation"] = 1.5 });
                Assert.Equal(2, kg.ActionLog.Count);
            }

            Assert.Single(kg.ActionLog);
        }
    }
}
