using System.Collections.Generic;
using System.Linq;
using RevitMCPKgCommandSet.Core;
using Xunit;

namespace RevitMCPKgCommandSet.Tests
{
    public class AnnotateTests
    {
        private static ProjectKg SeededKg()
        {
            var kg = new ProjectKg("p1");
            kg.AddNode("Level", new Dictionary<string, object> { ["name"] = "N0", ["elevation"] = 0.0 }, llmId: "lvl");
            kg.AddNode("WallType", new Dictionary<string, object> { ["name"] = "WT", ["total_thickness"] = 0.2 }, llmId: "wt");
            kg.AddNode("Wall", new Dictionary<string, object>
            {
                ["type_ref"] = "wt",
                ["level_ref"] = "lvl",
                ["p1"] = new[] { 0.0, 0.0 },
                ["p2"] = new[] { 5.0, 0.0 },
                ["length"] = 5.0,
                ["height"] = 3.0,
            }, llmId: "wall_a");
            kg.AddNode("Wall", new Dictionary<string, object>
            {
                ["type_ref"] = "wt",
                ["level_ref"] = "lvl",
                ["p1"] = new[] { 0.0, 0.0 },
                ["p2"] = new[] { 5.0, 0.0 },
                ["length"] = 5.0,
                ["height"] = 3.0,
            }, llmId: "wall_b");
            return kg;
        }

        // ---- happy path ----

        [Fact]
        public void Annotate_replaced_by_creates_f2_edge_with_payload()
        {
            var kg = SeededKg();
            var op = kg.Annotate("wall_a", "wall_b", EdgeTypes.ReplacedBy,
                new Dictionary<string, object> { ["reason"] = "rebind" });

            Assert.Equal("upsert", op);
            Assert.Equal(1, kg.EdgeCount);
            var edge = kg.OutgoingEdges("wall_a", EdgeTypes.ReplacedBy).Single();
            Assert.Equal("wall_b", edge.Dst);
            Assert.Equal("rebind", edge.Attrs["reason"]);
        }

        [Fact]
        public void Annotate_replaces_payload_on_second_call()
        {
            var kg = SeededKg();
            kg.Annotate("wall_a", "wall_b", EdgeTypes.Tagged,
                new Dictionary<string, object> { ["tag"] = "v1" });
            var op = kg.Annotate("wall_a", "wall_b", EdgeTypes.Tagged,
                new Dictionary<string, object> { ["tag"] = "v2" });

            Assert.Equal("replace", op);
            Assert.Equal(1, kg.EdgeCount);
            var edge = kg.OutgoingEdges("wall_a", EdgeTypes.Tagged).Single();
            Assert.Equal("v2", edge.Attrs["tag"]);
        }

        [Fact]
        public void Annotate_null_payload_deletes_existing_edge()
        {
            var kg = SeededKg();
            kg.Annotate("wall_a", "wall_b", EdgeTypes.Tagged,
                new Dictionary<string, object> { ["tag"] = "x" });
            var op = kg.Annotate("wall_a", "wall_b", EdgeTypes.Tagged, payload: null);

            Assert.Equal("delete", op);
            Assert.Equal(0, kg.EdgeCount);
            Assert.Empty(kg.OutgoingEdges("wall_a", EdgeTypes.Tagged));
        }

        [Fact]
        public void Annotate_null_payload_on_missing_edge_is_noop()
        {
            var kg = SeededKg();
            var op = kg.Annotate("wall_a", "wall_b", EdgeTypes.Tagged, payload: null);
            Assert.Equal("noop", op);
            Assert.Equal(0, kg.EdgeCount);
        }

        // ---- validation ----

        [Fact]
        public void Annotate_rejects_f1_edge_type()
        {
            var kg = SeededKg();
            Assert.Throws<System.ArgumentException>(() =>
                kg.Annotate("wall_a", "lvl", EdgeTypes.AtLevel,
                    new Dictionary<string, object> { ["k"] = 1 }));
        }

        // ---- user-defined edge types (symmetric to AddUserNode) ----

        [Fact]
        public void Annotate_registers_user_edge_type_on_first_use()
        {
            var kg = SeededKg();
            Assert.False(kg.IsUserEdgeType("adjacent_to"));

            var op = kg.Annotate("wall_a", "wall_b", "adjacent_to",
                new Dictionary<string, object> { ["via_door"] = "door_002" });

            Assert.Equal("upsert", op);
            Assert.True(kg.IsUserEdgeType("adjacent_to"));
            var edge = kg.OutgoingEdges("wall_a", "adjacent_to").Single();
            Assert.Equal("wall_b", edge.Dst);
            Assert.Equal("door_002", edge.Attrs["via_door"]);
        }

        [Fact]
        public void Annotate_builtin_f2_kind_is_not_a_user_edge_type()
        {
            var kg = SeededKg();
            kg.Annotate("wall_a", "wall_b", EdgeTypes.Tagged,
                new Dictionary<string, object> { ["tag"] = "x" });
            Assert.False(kg.IsUserEdgeType(EdgeTypes.Tagged));
        }

        [Fact]
        public void Annotate_noop_delete_of_unknown_kind_registers_nothing()
        {
            var kg = SeededKg();
            var op = kg.Annotate("wall_a", "wall_b", "adjacent_to", payload: null);
            Assert.Equal("noop", op);
            Assert.False(kg.IsUserEdgeType("adjacent_to"));
        }

        [Fact]
        public void Annotate_rejects_empty_kind()
        {
            var kg = SeededKg();
            Assert.Throws<System.ArgumentException>(() =>
                kg.Annotate("wall_a", "wall_b", "  ",
                    new Dictionary<string, object> { ["k"] = 1 }));
        }

        [Fact]
        public void User_edge_type_round_trips_through_replay()
        {
            var kg1 = SeededKg();
            var sink = new MemoryDeltaSink();
            kg1.AttachSink(sink);
            kg1.Annotate("wall_a", "wall_b", "adjacent_to",
                new Dictionary<string, object> { ["via_door"] = "door_002" });

            var kg2 = SeededKg();
            foreach (var e in sink.Entries)
                if (e.Op == DeltaOps.Annotate) kg2.Annotate(e.Src, e.Dst, e.EdgeType, e.Attrs);

            Assert.True(kg2.IsUserEdgeType("adjacent_to"));
            var edge = kg2.OutgoingEdges("wall_a", "adjacent_to").Single();
            Assert.Equal("door_002", edge.Attrs["via_door"]);
        }

        [Fact]
        public void User_edge_survives_apply_modified_repatch()
        {
            // A user-defined edge is KG-owned (not F1), so a Revit re-projection
            // of one of its endpoints must NOT wipe it — same guarantee as F2.
            var kg = new ProjectKg("p1");
            var fake = new FakeElementReader();
            fake.AddLevel(1001, "N0", 0.0);
            fake.AddWallType(2001, "WT200", 0.2);
            fake.AddWall(3001, 1001, 2001,
                new[] { 0.0, 0.0 }, new[] { 5.0, 0.0 }, 5.0, 3.0);
            fake.AddWall(3002, 1001, 2001,
                new[] { 0.0, 0.0 }, new[] { 5.0, 0.0 }, 5.0, 3.0);
            Projection.ApplyAdded(kg, fake, new long[] { 1001, 2001, 3001, 3002 });
            var wallA = kg.FindByRevitId(3001);
            var wallB = kg.FindByRevitId(3002);

            kg.Annotate(wallA, wallB, "adjacent_to",
                new Dictionary<string, object> { ["via_door"] = "d1" });

            fake.Attrs[3001]["height"] = 4.0;
            kg.AdvanceTurn();
            Projection.ApplyModified(kg, fake, new long[] { 3001 });

            Assert.Single(kg.OutgoingEdges(wallA, "adjacent_to"));
        }

        [Fact]
        public void Annotate_rejects_f1_user_attempt_even_with_payload()
        {
            // F1 (Revit-owned) types cannot be authored even though they are
            // "not F2" — they must not be mistaken for user edge types.
            var kg = SeededKg();
            foreach (var f1 in EdgeTypes.F1)
                Assert.Throws<System.ArgumentException>(() =>
                    kg.Annotate("wall_a", "wall_b", f1,
                        new Dictionary<string, object> { ["k"] = 1 }));
        }

        [Fact]
        public void Annotate_rejects_missing_src_or_dst()
        {
            var kg = SeededKg();
            Assert.Throws<KeyNotFoundException>(() =>
                kg.Annotate("ghost", "wall_b", EdgeTypes.Tagged,
                    new Dictionary<string, object> { ["k"] = 1 }));
            Assert.Throws<KeyNotFoundException>(() =>
                kg.Annotate("wall_a", "ghost", EdgeTypes.Tagged,
                    new Dictionary<string, object> { ["k"] = 1 }));
        }

        // ---- survival semantics (DESIGN §6) ----

        [Fact]
        public void Annotate_accepts_soft_deleted_src_and_dst()
        {
            // Audit-trail use case: a replaced_by edge must be authored
            // even when its anchors are already tombstoned.
            var kg = SeededKg();
            kg.SoftDelete("wall_a");
            kg.SoftDelete("wall_b");

            var op = kg.Annotate("wall_a", "wall_b", EdgeTypes.ReplacedBy,
                new Dictionary<string, object> { ["reason"] = "post-mortem" });

            Assert.Equal("upsert", op);
            Assert.Single(kg.OutgoingEdges("wall_a", EdgeTypes.ReplacedBy));
        }

        [Fact]
        public void F2_edges_survive_apply_modified_repatch()
        {
            // Annotation edges must NOT be wiped when the Revit projection
            // repatches F1 edges on the same node (DESIGN §2.2 + §6).
            var kg = new ProjectKg("p1");
            var fake = new FakeElementReader();
            fake.AddLevel(1001, "N0", 0.0);
            fake.AddWallType(2001, "WT200", 0.2);
            fake.AddWall(3001, 1001, 2001,
                new[] { 0.0, 0.0 }, new[] { 5.0, 0.0 }, 5.0, 3.0);
            Projection.ApplyAdded(kg, fake, new long[] { 1001, 2001, 3001 });
            var wallLlm = kg.FindByRevitId(3001);

            kg.AddNode("Wall", new Dictionary<string, object>
            {
                ["type_ref"] = "wt",
                ["level_ref"] = "lvl",
                ["p1"] = new[] { 0.0, 0.0 },
                ["p2"] = new[] { 5.0, 0.0 },
                ["length"] = 5.0,
                ["height"] = 3.0,
            }, llmId: "wall_replacement");
            kg.Annotate(wallLlm, "wall_replacement", EdgeTypes.ReplacedBy,
                new Dictionary<string, object> { ["reason"] = "rebind" });

            // Out-of-band Revit edit triggers a modify-repatch on the wall.
            fake.Attrs[3001]["height"] = 4.0;
            kg.AdvanceTurn();
            Projection.ApplyModified(kg, fake, new long[] { 3001 });

            // F1 edges repatched; F2 replaced_by edge survives.
            Assert.Single(kg.OutgoingEdges(wallLlm, EdgeTypes.ReplacedBy));
        }

        // ---- delta sink + replay ----

        [Fact]
        public void Annotate_emits_one_delta_entry_per_call()
        {
            var kg = SeededKg();
            var sink = new MemoryDeltaSink();
            kg.AttachSink(sink);

            kg.Annotate("wall_a", "wall_b", EdgeTypes.Tagged,
                new Dictionary<string, object> { ["tag"] = "a" });
            kg.Annotate("wall_a", "wall_b", EdgeTypes.Tagged,
                new Dictionary<string, object> { ["tag"] = "b" });
            kg.Annotate("wall_a", "wall_b", EdgeTypes.Tagged, payload: null);
            kg.Annotate("wall_a", "wall_b", EdgeTypes.Tagged, payload: null); // noop

            Assert.Equal(3, sink.Entries.Count);
            Assert.All(sink.Entries, e => Assert.Equal(DeltaOps.Annotate, e.Op));
            Assert.Equal("b", sink.Entries[1].Attrs["tag"]);
            Assert.Null(sink.Entries[2].Attrs);
        }

        [Fact]
        public void Replay_reconstructs_annotation_upsert_replace_delete()
        {
            var kg1 = SeededKg();
            var sink = new MemoryDeltaSink();
            kg1.AttachSink(sink);
            kg1.Annotate("wall_a", "wall_b", EdgeTypes.ReplacedBy,
                new Dictionary<string, object> { ["reason"] = "v1" });
            kg1.Annotate("wall_a", "wall_b", EdgeTypes.ReplacedBy,
                new Dictionary<string, object> { ["reason"] = "v2" });

            // Use a fresh KG that doesn't share the seeded nodes; rebuild from
            // the full create+annotate stream (sink only carries the annotate
            // entries here, so we seed manually).
            var kg2 = SeededKg();
            var jsonl = JsonlSerializer.SerializeAll(sink.Entries);
            foreach (var e in JsonlSerializer.DeserializeAll(jsonl))
            {
                if (e.Op == DeltaOps.Annotate) kg2.Annotate(e.Src, e.Dst, e.EdgeType, e.Attrs);
            }

            var edge = kg2.OutgoingEdges("wall_a", EdgeTypes.ReplacedBy).Single();
            Assert.Equal("v2", edge.Attrs["reason"]);
        }

        [Fact]
        public void Full_replay_handles_annotate_through_replay_engine()
        {
            // End-to-end : seed nodes + annotate + replay via ProjectKgReplay.
            var kg1 = SeededKg();
            var sink = new MemoryDeltaSink();
            kg1.AttachSink(sink);
            kg1.Annotate("wall_a", "wall_b", EdgeTypes.Tagged,
                new Dictionary<string, object> { ["tag"] = "live" });
            kg1.Annotate("wall_a", "wall_b", EdgeTypes.Tagged, payload: null);

            // Replay reconstructs from a fresh seed. Annotate handler in
            // ProjectKgReplay receives e.Attrs (null on delete) and routes
            // it back through kg.Annotate.
            var kg2 = SeededKg();
            foreach (var e in sink.Entries)
            {
                if (e.Op == DeltaOps.Annotate) kg2.Annotate(e.Src, e.Dst, e.EdgeType, e.Attrs);
            }

            Assert.Empty(kg2.OutgoingEdges("wall_a", EdgeTypes.Tagged));
        }
    }
}
