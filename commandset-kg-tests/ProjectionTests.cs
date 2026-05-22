using System.Collections.Generic;
using System.Linq;
using RevitMCPKgCommandSet.Core;
using Xunit;

namespace RevitMCPKgCommandSet.Tests
{
    internal sealed class FakeElementReader : IElementReader
    {
        public Dictionary<long, string> Types { get; } = new();
        public Dictionary<long, Dictionary<string, object>> Attrs { get; } = new();
        public Dictionary<long, List<EdgeSpec>> Edges { get; } = new();

        public string ResolveNodeType(long elementId) =>
            Types.TryGetValue(elementId, out var t) ? t : null;

        public Dictionary<string, object> ReadAttrs(long elementId) =>
            Attrs.TryGetValue(elementId, out var a) ? new Dictionary<string, object>(a) : null;

        public IEnumerable<EdgeSpec> ReadEdges(long elementId) =>
            Edges.TryGetValue(elementId, out var e) ? e : Enumerable.Empty<EdgeSpec>();

        public IEnumerable<long> EnumerateAllElementIds() => Types.Keys;

        public void Forget(long elementId)
        {
            Types.Remove(elementId);
            Attrs.Remove(elementId);
            Edges.Remove(elementId);
        }

        public void AddLevel(long eid, string name, double elev)
        {
            Types[eid] = "Level";
            Attrs[eid] = new Dictionary<string, object> { ["name"] = name, ["elevation"] = elev };
            Edges[eid] = new List<EdgeSpec>();
        }

        public void AddWallType(long eid, string name, double thickness)
        {
            Types[eid] = "WallType";
            Attrs[eid] = new Dictionary<string, object> { ["name"] = name, ["total_thickness"] = thickness };
            Edges[eid] = new List<EdgeSpec>();
        }

        public void AddWall(long eid, long levelEid, long wallTypeEid, double[] p1, double[] p2, double length, double height)
        {
            Types[eid] = "Wall";
            Attrs[eid] = new Dictionary<string, object>
            {
                ["type_ref"] = $"walltype_revit_{wallTypeEid}",
                ["level_ref"] = $"level_revit_{levelEid}",
                ["p1"] = p1,
                ["p2"] = p2,
                ["length"] = length,
                ["height"] = height,
            };
            Edges[eid] = new List<EdgeSpec>
            {
                new EdgeSpec(levelEid, EdgeTypes.AtLevel),
                new EdgeSpec(wallTypeEid, EdgeTypes.IsType),
            };
        }

        public void AddRoom(long eid, long levelEid, string name, double area, params long[] boundaryWallEids)
        {
            Types[eid] = "Room";
            var attrs = new Dictionary<string, object>
            {
                ["name"] = name,
                ["level_ref"] = $"revit_{levelEid}",
                ["area"] = area,
            };
            if (boundaryWallEids.Length > 0)
                attrs["boundary_walls"] = boundaryWallEids.Select(w => $"revit_{w}").ToList();
            Attrs[eid] = attrs;

            var edges = new List<EdgeSpec> { new EdgeSpec(levelEid, EdgeTypes.AtLevel) };
            edges.AddRange(boundaryWallEids.Select(w => new EdgeSpec(w, EdgeTypes.BoundedBy)));
            Edges[eid] = edges;
        }
    }

    public class ProjectionTests
    {
        [Fact]
        public void ApplyAdded_creates_node_with_revit_binding()
        {
            var kg = new ProjectKg("p1");
            var fake = new FakeElementReader();
            fake.AddLevel(1001, "N0", 0.0);

            var stats = Projection.ApplyAdded(kg, fake, new long[] { 1001 });
            Assert.Equal(1, stats.NodesAffected);
            Assert.Equal(0, stats.EdgesAffected);
            Assert.Equal(1, kg.NodeCount);
            Assert.Equal("level_001", kg.FindByRevitId(1001));
        }

        [Fact]
        public void ApplyAdded_projects_room_with_level_and_boundary_edges()
        {
            // Regression: Room was schema-declared but never projected, so
            // kg_v2_query type=Room returned 0 nodes. The reader now emits
            // Room attrs + at_level/bounded_by edges; this asserts the
            // generic pipeline accepts them and wires both edge kinds.
            var kg = new ProjectKg("p1");
            var fake = new FakeElementReader();
            fake.AddLevel(1001, "N0", 0.0);
            fake.AddWallType(2001, "WT200", 0.2);
            fake.AddWall(3001, 1001, 2001, new[] { 0.0, 0.0 }, new[] { 5.0, 0.0 }, 5.0, 3.0);
            fake.AddWall(3002, 1001, 2001, new[] { 5.0, 0.0 }, new[] { 5.0, 5.0 }, 5.0, 3.0);
            fake.AddRoom(4001, levelEid: 1001, name: "Bureau", area: 25.0,
                boundaryWallEids: new long[] { 3001, 3002 });

            var stats = Projection.ApplyAdded(kg, fake, new long[] { 1001, 2001, 3001, 3002, 4001 });

            var roomLlmId = kg.FindByRevitId(4001);
            Assert.NotNull(roomLlmId);
            Assert.Equal("Room", kg.GetNode(roomLlmId).NodeType);
            Assert.Single(kg.OutgoingEdges(roomLlmId, EdgeTypes.AtLevel));
            Assert.Equal(2, kg.OutgoingEdges(roomLlmId, EdgeTypes.BoundedBy).Count());
            Assert.Single(kg.NodesOfType("Room"));
        }

        [Fact]
        public void ApplyAdded_skips_unknown_node_types()
        {
            var kg = new ProjectKg("p1");
            var fake = new FakeElementReader();
            fake.AddLevel(1001, "N0", 0.0);

            var stats = Projection.ApplyAdded(kg, fake, new long[] { 1001, 9999 });
            Assert.Equal(1, stats.NodesAffected);
            Assert.Equal(1, stats.Skipped);
        }

        [Fact]
        public void ApplyAdded_two_pass_resolves_edges_within_batch()
        {
            var kg = new ProjectKg("p1");
            var fake = new FakeElementReader();
            fake.AddLevel(1001, "N0", 0.0);
            fake.AddWallType(2001, "WT200", 0.2);
            fake.AddWall(3001, levelEid: 1001, wallTypeEid: 2001,
                p1: new[] { 0.0, 0.0 }, p2: new[] { 5.0, 0.0 },
                length: 5.0, height: 3.0);

            var stats = Projection.ApplyAdded(kg, fake, new long[] { 1001, 2001, 3001 });
            Assert.Equal(3, stats.NodesAffected);
            Assert.Equal(2, stats.EdgesAffected);

            var wallLlmId = kg.FindByRevitId(3001);
            var outgoing = kg.OutgoingEdges(wallLlmId).ToList();
            Assert.Equal(2, outgoing.Count);
            Assert.Contains(outgoing, e => e.EdgeType == EdgeTypes.AtLevel);
            Assert.Contains(outgoing, e => e.EdgeType == EdgeTypes.IsType);
        }

        [Fact]
        public void ApplyAdded_resolves_edges_to_preexisting_nodes()
        {
            var kg = new ProjectKg("p1");
            var fake = new FakeElementReader();
            fake.AddLevel(1001, "N0", 0.0);
            fake.AddWallType(2001, "WT200", 0.2);
            Projection.ApplyAdded(kg, fake, new long[] { 1001, 2001 });

            fake.AddWall(3001, 1001, 2001, new[] { 0.0, 0.0 }, new[] { 5.0, 0.0 }, 5.0, 3.0);
            var stats = Projection.ApplyAdded(kg, fake, new long[] { 3001 });

            Assert.Equal(1, stats.NodesAffected);
            Assert.Equal(2, stats.EdgesAffected);
        }

        [Fact]
        public void ApplyAdded_drops_edges_to_unknown_targets()
        {
            var kg = new ProjectKg("p1");
            var fake = new FakeElementReader();
            fake.AddWall(3001, levelEid: 9999, wallTypeEid: 9998,
                p1: new[] { 0.0, 0.0 }, p2: new[] { 5.0, 0.0 }, length: 5.0, height: 3.0);

            var stats = Projection.ApplyAdded(kg, fake, new long[] { 3001 });
            Assert.Equal(1, stats.NodesAffected);
            Assert.Equal(0, stats.EdgesAffected);
        }

        [Fact]
        public void ApplyDeleted_soft_deletes_tracked_node()
        {
            var kg = new ProjectKg("p1");
            var fake = new FakeElementReader();
            fake.AddLevel(1001, "N0", 0.0);
            Projection.ApplyAdded(kg, fake, new long[] { 1001 });

            var stats = Projection.ApplyDeleted(kg, new long[] { 1001 });
            Assert.Equal(1, stats.NodesAffected);
            Assert.True(kg.GetNode("level_001").IsSoftDeleted);
        }

        [Fact]
        public void ApplyDeleted_skips_unknown_element_ids()
        {
            var kg = new ProjectKg("p1");
            var stats = Projection.ApplyDeleted(kg, new long[] { 9999 });
            Assert.Equal(0, stats.NodesAffected);
            Assert.Equal(1, stats.Skipped);
        }

        [Fact]
        public void ApplyModified_updates_changed_attrs()
        {
            var kg = new ProjectKg("p1");
            var fake = new FakeElementReader();
            fake.AddLevel(1001, "N0", 0.0);
            Projection.ApplyAdded(kg, fake, new long[] { 1001 });

            fake.Attrs[1001]["elevation"] = 3.5;
            kg.AdvanceTurn();

            var stats = Projection.ApplyModified(kg, fake, new long[] { 1001 });
            Assert.Equal(1, stats.NodesAffected);
            Assert.Equal(3.5, kg.GetNode("level_001").Attrs["elevation"]);
        }

        [Fact]
        public void ApplyModified_skips_unchanged_attrs()
        {
            var kg = new ProjectKg("p1");
            var fake = new FakeElementReader();
            fake.AddLevel(1001, "N0", 0.0);
            Projection.ApplyAdded(kg, fake, new long[] { 1001 });

            var stats = Projection.ApplyModified(kg, fake, new long[] { 1001 });
            Assert.Equal(0, stats.NodesAffected);
        }

        [Fact]
        public void ApplyModified_repatches_f1_edges_but_keeps_annotation_edges()
        {
            var kg = new ProjectKg("p1");
            var fake = new FakeElementReader();
            fake.AddLevel(1001, "N0", 0.0);
            fake.AddLevel(1002, "N1", 3.0);
            fake.AddWallType(2001, "WT200", 0.2);
            fake.AddWall(3001, levelEid: 1001, wallTypeEid: 2001,
                p1: new[] { 0.0, 0.0 }, p2: new[] { 5.0, 0.0 }, length: 5.0, height: 3.0);
            Projection.ApplyAdded(kg, fake, new long[] { 1001, 1002, 2001, 3001 });

            var wallLlmId = kg.FindByRevitId(3001);
            kg.AddNode("Level", new Dictionary<string, object> { ["name"] = "Sentinel", ["elevation"] = 99.0 }, llmId: "sentinel");

            fake.Edges[3001] = new List<EdgeSpec>
            {
                new EdgeSpec(1002, EdgeTypes.AtLevel),
                new EdgeSpec(2001, EdgeTypes.IsType),
            };
            kg.AdvanceTurn();
            Projection.ApplyModified(kg, fake, new long[] { 3001 });

            var atLevels = kg.OutgoingEdges(wallLlmId, EdgeTypes.AtLevel).ToList();
            Assert.Single(atLevels);
            Assert.Equal(kg.FindByRevitId(1002), atLevels[0].Dst);
        }

        [Fact]
        public void ApplyAdded_incoming_edge_wires_peer_as_source()
        {
            var kg = new ProjectKg("p1");
            var fake = new FakeElementReader();
            fake.AddLevel(1001, "N0", 0.0);
            fake.AddWallType(2001, "WT200", 0.2);
            fake.AddWall(3001, levelEid: 1001, wallTypeEid: 2001,
                p1: new[] { 0.0, 0.0 }, p2: new[] { 5.0, 0.0 }, length: 5.0, height: 3.0);
            Projection.ApplyAdded(kg, fake, new long[] { 1001, 2001, 3001 });

            fake.Types[4001] = "Window";
            fake.Attrs[4001] = new Dictionary<string, object>
            {
                ["type_ref"] = "revit_5001",
                ["host_wall_ref"] = "revit_3001",
                ["position"] = new[] { 2.5, 0.0 },
                ["sill_height"] = 0.9,
                ["head_height"] = 2.1,
            };
            fake.Edges[4001] = new List<EdgeSpec>
            {
                new EdgeSpec(3001, EdgeTypes.Hosts, EdgeDirection.Incoming),
            };

            var stats = Projection.ApplyAdded(kg, fake, new long[] { 4001 });

            Assert.Equal(1, stats.NodesAffected);
            Assert.Equal(1, stats.EdgesAffected);
            var windowLlmId = kg.FindByRevitId(4001);
            var wallLlmId = kg.FindByRevitId(3001);
            var hostsEdges = kg.OutgoingEdges(wallLlmId, EdgeTypes.Hosts).ToList();
            Assert.Single(hostsEdges);
            Assert.Equal(windowLlmId, hostsEdges[0].Dst);
        }

        [Fact]
        public void ApplyModified_skips_soft_deleted_nodes()
        {
            var kg = new ProjectKg("p1");
            var fake = new FakeElementReader();
            fake.AddLevel(1001, "N0", 0.0);
            Projection.ApplyAdded(kg, fake, new long[] { 1001 });
            Projection.ApplyDeleted(kg, new long[] { 1001 });

            fake.Attrs[1001]["elevation"] = 99.0;
            var stats = Projection.ApplyModified(kg, fake, new long[] { 1001 });
            Assert.Equal(0, stats.NodesAffected);
            Assert.Equal(1, stats.Skipped);
        }

        // ---- undo/redo robustness (P3) ----

        [Fact]
        public void ApplyAdded_resurrects_soft_deleted_node_by_revit_id()
        {
            // Scenario: Ctrl+Z on a delete. Revit re-creates the element with
            // the same ElementId; the projection must preserve llm_id and
            // not forge a duplicate.
            var kg = new ProjectKg("p1");
            var fake = new FakeElementReader();
            fake.AddLevel(1001, "N0", 0.0);
            Projection.ApplyAdded(kg, fake, new long[] { 1001 });
            var originalLlmId = kg.FindByRevitId(1001);
            Projection.ApplyDeleted(kg, new long[] { 1001 });
            Assert.True(kg.GetNode(originalLlmId).IsSoftDeleted);

            var stats = Projection.ApplyAdded(kg, fake, new long[] { 1001 });

            Assert.Equal(1, stats.NodesAffected);
            Assert.Equal(0, stats.Skipped);
            Assert.Equal(1, kg.NodeCount);
            Assert.Equal(originalLlmId, kg.FindByRevitId(1001));
            Assert.False(kg.GetNode(originalLlmId).IsSoftDeleted);
        }

        [Fact]
        public void ApplyAdded_resurrect_syncs_attrs_changed_since_tombstone()
        {
            // Scenario: redo path where Revit re-creates the element with
            // updated attrs (e.g. Ctrl+Y after attrs changed between
            // delete and re-create).
            var kg = new ProjectKg("p1");
            var fake = new FakeElementReader();
            fake.AddLevel(1001, "N0", 0.0);
            Projection.ApplyAdded(kg, fake, new long[] { 1001 });
            Projection.ApplyDeleted(kg, new long[] { 1001 });

            fake.Attrs[1001]["elevation"] = 3.5;
            kg.AdvanceTurn();
            Projection.ApplyAdded(kg, fake, new long[] { 1001 });

            var llmId = kg.FindByRevitId(1001);
            Assert.False(kg.GetNode(llmId).IsSoftDeleted);
            Assert.Equal(3.5, kg.GetNode(llmId).Attrs["elevation"]);
        }

        [Fact]
        public void ApplyAdded_resurrect_repatches_f1_edges()
        {
            // Scenario: a wall was hosted on level N0, deleted, then re-created
            // by undo while pointing at a different level. Edges must reflect
            // the current state, not the stale tombstone.
            var kg = new ProjectKg("p1");
            var fake = new FakeElementReader();
            fake.AddLevel(1001, "N0", 0.0);
            fake.AddLevel(1002, "N1", 3.0);
            fake.AddWallType(2001, "WT200", 0.2);
            fake.AddWall(3001, levelEid: 1001, wallTypeEid: 2001,
                p1: new[] { 0.0, 0.0 }, p2: new[] { 5.0, 0.0 }, length: 5.0, height: 3.0);
            Projection.ApplyAdded(kg, fake, new long[] { 1001, 1002, 2001, 3001 });

            var wallLlmId = kg.FindByRevitId(3001);
            Projection.ApplyDeleted(kg, new long[] { 3001 });

            // Wall comes back, now at N1.
            fake.Attrs[3001]["level_ref"] = "level_revit_1002";
            fake.Edges[3001] = new List<EdgeSpec>
            {
                new EdgeSpec(1002, EdgeTypes.AtLevel),
                new EdgeSpec(2001, EdgeTypes.IsType),
            };
            kg.AdvanceTurn();
            Projection.ApplyAdded(kg, fake, new long[] { 3001 });

            Assert.Equal(wallLlmId, kg.FindByRevitId(3001));
            Assert.False(kg.GetNode(wallLlmId).IsSoftDeleted);
            var atLevels = kg.OutgoingEdges(wallLlmId, EdgeTypes.AtLevel).ToList();
            Assert.Single(atLevels);
            Assert.Equal(kg.FindByRevitId(1002), atLevels[0].Dst);
        }

        [Fact]
        public void ApplyAdded_undo_redo_cycle_keeps_single_node()
        {
            // Three full cycles of create→delete→resurrect. Node count and
            // llm_id must stay stable.
            var kg = new ProjectKg("p1");
            var fake = new FakeElementReader();
            fake.AddLevel(1001, "N0", 0.0);
            Projection.ApplyAdded(kg, fake, new long[] { 1001 });
            var llmId = kg.FindByRevitId(1001);

            for (int i = 0; i < 3; i++)
            {
                Projection.ApplyDeleted(kg, new long[] { 1001 });
                Assert.True(kg.GetNode(llmId).IsSoftDeleted);
                Projection.ApplyAdded(kg, fake, new long[] { 1001 });
                Assert.False(kg.GetNode(llmId).IsSoftDeleted);
            }

            Assert.Equal(1, kg.NodeCount);
            Assert.Equal(llmId, kg.FindByRevitId(1001));
        }

        [Fact]
        public void ApplyAdded_skips_when_live_node_with_same_revit_id_exists()
        {
            // Defensive: Revit doesn't reuse live ElementIds, but the
            // projection must refuse a duplicate-add silently rather than
            // forge a second node bound to the same revit_id.
            var kg = new ProjectKg("p1");
            var fake = new FakeElementReader();
            fake.AddLevel(1001, "N0", 0.0);
            Projection.ApplyAdded(kg, fake, new long[] { 1001 });

            var stats = Projection.ApplyAdded(kg, fake, new long[] { 1001 });

            Assert.Equal(0, stats.NodesAffected);
            Assert.Equal(1, stats.Skipped);
            Assert.Equal(1, kg.NodeCount);
        }
    }
}
