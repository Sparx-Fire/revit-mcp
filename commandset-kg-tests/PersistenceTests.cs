using System.Collections.Generic;
using System.Linq;
using RevitMCPKgCommandSet.Core;
using Xunit;

namespace RevitMCPKgCommandSet.Tests
{
    public class PersistenceTests
    {
        private static Dictionary<string, object> LevelAttrs(string name = "N0", double elev = 0.0) =>
            new() { ["name"] = name, ["elevation"] = elev };

        private static Dictionary<string, object> WallAttrs() =>
            new()
            {
                ["type_ref"] = "walltype_001",
                ["level_ref"] = "level_001",
                ["p1"] = new[] { 0.0, 0.0 },
                ["p2"] = new[] { 5.0, 0.0 },
                ["length"] = 5.0,
                ["height"] = 3.0,
            };

        // ---- emission ----

        [Fact]
        public void AddNode_emits_create_node_entry()
        {
            var kg = new ProjectKg("p1");
            var sink = new MemoryDeltaSink();
            kg.AttachSink(sink);

            kg.AddNode("Level", LevelAttrs());

            Assert.Single(sink.Entries);
            var e = sink.Entries[0];
            Assert.Equal(DeltaOps.CreateNode, e.Op);
            Assert.Equal("level_001", e.Id);
            Assert.Equal("Level", e.NodeType);
            Assert.Equal("N0", e.Attrs["name"]);
        }

        [Fact]
        public void AdvanceTurn_emits_advance_turn_entry()
        {
            var kg = new ProjectKg("p1");
            var sink = new MemoryDeltaSink();
            kg.AttachSink(sink);

            kg.AdvanceTurn();
            kg.AdvanceTurn();

            Assert.Equal(2, sink.Entries.Count);
            Assert.All(sink.Entries, e => Assert.Equal(DeltaOps.AdvanceTurn, e.Op));
        }

        [Fact]
        public void ModifyNode_emits_modify_entry()
        {
            var kg = new ProjectKg("p1");
            kg.AddNode("Level", LevelAttrs(), llmId: "lvl");
            var sink = new MemoryDeltaSink();
            kg.AttachSink(sink);

            kg.AdvanceTurn();
            kg.ModifyNode("lvl", new Dictionary<string, object> { ["elevation"] = 1.5 });

            Assert.Equal(2, sink.Entries.Count);
            Assert.Equal(DeltaOps.AdvanceTurn, sink.Entries[0].Op);
            Assert.Equal(DeltaOps.ModifyNode, sink.Entries[1].Op);
            Assert.Equal(1.5, sink.Entries[1].Updates["elevation"]);
        }

        [Fact]
        public void SoftDelete_emits_soft_delete_entry()
        {
            var kg = new ProjectKg("p1");
            kg.AddNode("Level", LevelAttrs(), llmId: "lvl");
            var sink = new MemoryDeltaSink();
            kg.AttachSink(sink);

            kg.SoftDelete("lvl");

            Assert.Single(sink.Entries);
            Assert.Equal(DeltaOps.SoftDelete, sink.Entries[0].Op);
            Assert.Equal("lvl", sink.Entries[0].Id);
        }

        [Fact]
        public void SetRevitId_emits_set_revit_id_entry()
        {
            var kg = new ProjectKg("p1");
            kg.AddNode("Level", LevelAttrs(), llmId: "lvl");
            var sink = new MemoryDeltaSink();
            kg.AttachSink(sink);

            kg.SetRevitId("lvl", 12345);

            Assert.Single(sink.Entries);
            Assert.Equal(DeltaOps.SetRevitId, sink.Entries[0].Op);
            Assert.Equal(12345L, sink.Entries[0].RevitId);
        }

        [Fact]
        public void AddEdge_emits_add_edge_entry_only_when_new()
        {
            var kg = new ProjectKg("p1");
            kg.AddNode("Level", LevelAttrs(), llmId: "lvl");
            kg.AddNode("WallType", new Dictionary<string, object> { ["name"] = "WT", ["total_thickness"] = 0.2 }, llmId: "wt");
            kg.AddNode("Wall", WallAttrs(), llmId: "wall");
            var sink = new MemoryDeltaSink();
            kg.AttachSink(sink);

            Assert.True(kg.AddEdge("wall", "lvl", EdgeTypes.AtLevel));
            Assert.False(kg.AddEdge("wall", "lvl", EdgeTypes.AtLevel));

            Assert.Single(sink.Entries);
            Assert.Equal(DeltaOps.AddEdge, sink.Entries[0].Op);
        }

        [Fact]
        public void RemoveEdge_emits_only_when_present()
        {
            var kg = new ProjectKg("p1");
            kg.AddNode("Level", LevelAttrs(), llmId: "lvl");
            kg.AddNode("WallType", new Dictionary<string, object> { ["name"] = "WT", ["total_thickness"] = 0.2 }, llmId: "wt");
            kg.AddNode("Wall", WallAttrs(), llmId: "wall");
            kg.AddEdge("wall", "lvl", EdgeTypes.AtLevel);
            var sink = new MemoryDeltaSink();
            kg.AttachSink(sink);

            Assert.True(kg.RemoveEdge("wall", "lvl", EdgeTypes.AtLevel));
            Assert.False(kg.RemoveEdge("wall", "lvl", EdgeTypes.AtLevel));

            Assert.Single(sink.Entries);
            Assert.Equal(DeltaOps.RemoveEdge, sink.Entries[0].Op);
        }

        [Fact]
        public void Detached_sink_means_no_emission()
        {
            var kg = new ProjectKg("p1");
            var sink = new MemoryDeltaSink();
            kg.AttachSink(sink);
            kg.AddNode("Level", LevelAttrs(), llmId: "lvl");
            kg.DetachSink();

            kg.AdvanceTurn();
            kg.SoftDelete("lvl");

            Assert.Single(sink.Entries);
        }

        // ---- JSONL round-trip ----

        [Fact]
        public void Jsonl_round_trips_one_entry()
        {
            var e = new DeltaEntry
            {
                Turn = 7,
                Op = DeltaOps.CreateNode,
                Id = "wall_001",
                NodeType = "Wall",
                Attrs = new Dictionary<string, object>
                {
                    ["length"] = 5.0,
                    ["height"] = 3.0,
                    ["p1"] = new[] { 0.0, 0.0 },
                },
            };
            var line = JsonlSerializer.SerializeOne(e);
            var back = JsonlSerializer.DeserializeOne(line);

            Assert.Equal(e.Op, back.Op);
            Assert.Equal(e.Id, back.Id);
            Assert.Equal(e.NodeType, back.NodeType);
            Assert.Equal(5.0, back.Attrs["length"]);
            var p1 = (object[])back.Attrs["p1"];
            Assert.Equal(2, p1.Length);
        }

        [Fact]
        public void Jsonl_round_trips_many_entries()
        {
            var entries = new List<DeltaEntry>
            {
                new() { Turn = 1, Op = DeltaOps.AdvanceTurn },
                new() { Turn = 1, Op = DeltaOps.CreateNode, Id = "a", NodeType = "Level",
                       Attrs = new Dictionary<string, object> { ["name"] = "N0", ["elevation"] = 0.0 } },
                new() { Turn = 2, Op = DeltaOps.SoftDelete, Id = "a" },
            };
            var s = JsonlSerializer.SerializeAll(entries);
            var back = JsonlSerializer.DeserializeAll(s).ToList();

            Assert.Equal(3, back.Count);
            Assert.Equal(DeltaOps.AdvanceTurn, back[0].Op);
            Assert.Equal("a", back[1].Id);
            Assert.Equal(DeltaOps.SoftDelete, back[2].Op);
        }

        [Fact]
        public void Jsonl_ignores_blank_lines()
        {
            var s = "\n\n" +
                    JsonlSerializer.SerializeOne(new DeltaEntry { Turn = 1, Op = DeltaOps.AdvanceTurn }) + "\n" +
                    "\n";
            var back = JsonlSerializer.DeserializeAll(s).ToList();
            Assert.Single(back);
        }

        // ---- replay ----

        [Fact]
        public void Replay_reconstructs_empty_kg()
        {
            var (kg, stats) = ProjectKgReplay.Replay("p1", new List<DeltaEntry>());
            Assert.Equal(0, kg.NodeCount);
            Assert.Equal(0, kg.EdgeCount);
            Assert.Equal(0, stats.Applied);
        }

        [Fact]
        public void Replay_reconstructs_state_after_create_modify_delete()
        {
            var kg1 = new ProjectKg("p1");
            var sink = new MemoryDeltaSink();
            kg1.AttachSink(sink);
            kg1.AdvanceTurn();
            kg1.AddNode("Level", LevelAttrs(elev: 0.0), llmId: "lvl");
            kg1.AdvanceTurn();
            kg1.ModifyNode("lvl", new Dictionary<string, object> { ["elevation"] = 3.0 });
            kg1.AdvanceTurn();
            kg1.SoftDelete("lvl");

            var (kg2, stats) = ProjectKgReplay.Replay("p1", sink.Entries);

            Assert.Equal(stats.Applied, sink.Count);
            Assert.Equal(0, stats.Skipped);
            Assert.Equal(1, kg2.NodeCount);
            Assert.Equal(3, kg2.Turn);
            Assert.True(kg2.GetNode("lvl").IsSoftDeleted);
            Assert.Equal(3.0, kg2.GetNode("lvl").Attrs["elevation"]);
        }

        [Fact]
        public void Replay_reconstructs_edges_and_revit_binding()
        {
            var kg1 = new ProjectKg("p1");
            var sink = new MemoryDeltaSink();
            kg1.AttachSink(sink);
            kg1.AddNode("Level", LevelAttrs(), llmId: "lvl");
            kg1.SetRevitId("lvl", 100);
            kg1.AddNode("WallType", new Dictionary<string, object> { ["name"] = "WT", ["total_thickness"] = 0.2 }, llmId: "wt");
            kg1.SetRevitId("wt", 200);
            kg1.AddNode("Wall", WallAttrs(), llmId: "wall");
            kg1.SetRevitId("wall", 300);
            kg1.AddEdge("wall", "lvl", EdgeTypes.AtLevel);
            kg1.AddEdge("wall", "wt", EdgeTypes.IsType);

            var (kg2, _) = ProjectKgReplay.Replay("p1", sink.Entries);

            Assert.Equal(3, kg2.NodeCount);
            Assert.Equal(2, kg2.EdgeCount);
            Assert.Equal("lvl", kg2.FindByRevitId(100));
            Assert.Equal("wall", kg2.FindByRevitId(300));
        }

        [Fact]
        public void Replay_through_jsonl_round_trip_is_lossless()
        {
            var kg1 = new ProjectKg("p1");
            var sink = new MemoryDeltaSink();
            kg1.AttachSink(sink);
            kg1.AdvanceTurn();
            kg1.AddNode("Level", LevelAttrs(), llmId: "lvl");
            kg1.SetRevitId("lvl", 100);
            kg1.AddNode("WallType", new Dictionary<string, object> { ["name"] = "WT", ["total_thickness"] = 0.2 }, llmId: "wt");
            kg1.AddNode("Wall", WallAttrs(), llmId: "wall");
            kg1.AddEdge("wall", "lvl", EdgeTypes.AtLevel);

            var jsonl = JsonlSerializer.SerializeAll(sink.Entries);
            var parsed = JsonlSerializer.DeserializeAll(jsonl);
            var (kg2, stats) = ProjectKgReplay.Replay("p1", parsed);

            Assert.Equal(0, stats.Skipped);
            Assert.Equal(3, kg2.NodeCount);
            Assert.Equal(1, kg2.EdgeCount);
            Assert.Equal(1, kg2.Turn);
            Assert.Equal("lvl", kg2.FindByRevitId(100));
        }

        [Fact]
        public void Replay_skips_unsupported_op_with_reason()
        {
            var entries = new List<DeltaEntry>
            {
                new() { Turn = 1, Op = DeltaOps.AdvanceTurn },
                new() { Turn = 1, Op = "bogus_future_op", Id = "x" },
                new() { Turn = 2, Op = DeltaOps.AdvanceTurn },
            };
            var (kg, stats) = ProjectKgReplay.Replay("p1", entries);
            Assert.Equal(2, stats.Applied);
            Assert.Equal(1, stats.Skipped);
            Assert.Equal(2, kg.Turn);
        }

        [Fact]
        public void Replay_does_not_emit_to_a_new_sink()
        {
            var kg1 = new ProjectKg("p1");
            var sink1 = new MemoryDeltaSink();
            kg1.AttachSink(sink1);
            kg1.AddNode("Level", LevelAttrs(), llmId: "lvl");

            var (kg2, _) = ProjectKgReplay.Replay("p1", sink1.Entries);
            var sink2 = new MemoryDeltaSink();
            kg2.AttachSink(sink2);

            Assert.Equal(0, sink2.Count);
        }

        // ---- resurrect (P3 undo/redo robustness) ----

        [Fact]
        public void Resurrect_emits_resurrect_entry_and_clears_tombstone()
        {
            var kg = new ProjectKg("p1");
            kg.AddNode("Level", LevelAttrs(), llmId: "lvl");
            kg.SoftDelete("lvl");
            Assert.True(kg.GetNode("lvl").IsSoftDeleted);

            var sink = new MemoryDeltaSink();
            kg.AttachSink(sink);
            kg.Resurrect("lvl");

            Assert.False(kg.GetNode("lvl").IsSoftDeleted);
            Assert.Single(sink.Entries);
            Assert.Equal(DeltaOps.Resurrect, sink.Entries[0].Op);
            Assert.Equal("lvl", sink.Entries[0].Id);
        }

        [Fact]
        public void Resurrect_on_live_node_is_noop()
        {
            var kg = new ProjectKg("p1");
            kg.AddNode("Level", LevelAttrs(), llmId: "lvl");
            var sink = new MemoryDeltaSink();
            kg.AttachSink(sink);

            kg.Resurrect("lvl");

            Assert.Equal(0, sink.Count);
        }

        [Fact]
        public void Replay_handles_create_delete_resurrect_sequence()
        {
            var kg1 = new ProjectKg("p1");
            var sink = new MemoryDeltaSink();
            kg1.AttachSink(sink);
            kg1.AdvanceTurn();
            kg1.AddNode("Level", LevelAttrs(), llmId: "lvl");
            kg1.AdvanceTurn();
            kg1.SoftDelete("lvl");
            kg1.AdvanceTurn();
            kg1.Resurrect("lvl");

            var jsonl = JsonlSerializer.SerializeAll(sink.Entries);
            var parsed = JsonlSerializer.DeserializeAll(jsonl);
            var (kg2, stats) = ProjectKgReplay.Replay("p1", parsed);

            Assert.Equal(0, stats.Skipped);
            Assert.Equal(1, kg2.NodeCount);
            Assert.False(kg2.GetNode("lvl").IsSoftDeleted);
        }
    }
}
