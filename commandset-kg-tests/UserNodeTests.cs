using System.Collections.Generic;
using System.Linq;
using RevitMCPKgCommandSet.Core;
using Xunit;

namespace RevitMCPKgCommandSet.Tests
{
    // User-defined semantic node types (e.g. Suite) authored at runtime:
    // free-form attrs, no Revit binding, persisted + replayed via the
    // create_user_node delta.
    public class UserNodeTests
    {
        [Fact]
        public void AddUserNode_creates_freeform_node_and_registers_type()
        {
            var kg = new ProjectKg("p1");
            kg.AdvanceTurn();
            var id = kg.AddUserNode("Suite", new Dictionary<string, object>
            {
                ["name"] = "Suite A",
                ["program"] = "residential",
            });

            Assert.True(kg.IsUserType("Suite"));
            Assert.Equal("suite_001", id);
            var node = kg.GetNode(id);
            Assert.Equal("Suite", node.NodeType);
            Assert.Equal("Suite A", node.Attrs["name"]);
            Assert.Equal("residential", node.Attrs["program"]);
            Assert.Null(node.RevitId);
            Assert.True(node.CreatedAtTurn > 0);
        }

        [Fact]
        public void AddUserNode_rejects_builtin_type_name()
        {
            var kg = new ProjectKg("p1");
            var ex = Assert.Throws<System.ArgumentException>(() =>
                kg.AddUserNode("Wall", new Dictionary<string, object>()));
            Assert.Contains("built-in", ex.Message);
        }

        [Fact]
        public void ModifyNode_on_user_node_accepts_arbitrary_attrs()
        {
            var kg = new ProjectKg("p1");
            var id = kg.AddUserNode("Suite", new Dictionary<string, object> { ["name"] = "A" });
            kg.AdvanceTurn();
            kg.ModifyNode(id, new Dictionary<string, object>
            {
                ["name"] = "A-renamed",
                ["floor_count"] = 3, // brand-new key, no schema needed
            });

            var node = kg.GetNode(id);
            Assert.Equal("A-renamed", node.Attrs["name"]);
            Assert.Equal(3, node.Attrs["floor_count"]);
        }

        [Fact]
        public void Contains_edge_links_user_node_to_existing_node()
        {
            var kg = new ProjectKg("p1");
            var suite = kg.AddUserNode("Suite", new Dictionary<string, object> { ["name"] = "A" });
            var room = kg.AddNode("Room", new Dictionary<string, object>
            {
                ["name"] = "Bureau",
                ["level_ref"] = "revit_1001",
            });

            var op = kg.Annotate(suite, room, EdgeTypes.Contains,
                new Dictionary<string, object> { ["since"] = "phase1" });

            Assert.Equal("upsert", op);
            var edges = kg.OutgoingEdges(suite, EdgeTypes.Contains).ToList();
            Assert.Single(edges);
            Assert.Equal(room, edges[0].Dst);
        }

        [Fact]
        public void CreateUserNode_round_trips_through_replay()
        {
            var kg = new ProjectKg("p1");
            var sink = new MemoryDeltaSink();
            kg.AttachSink(sink);

            kg.AdvanceTurn();
            var suite = kg.AddUserNode("Suite", new Dictionary<string, object>
            {
                ["name"] = "A",
                ["tags"] = new List<object> { "vip", "corner" },
            });
            var room = kg.AddNode("Room", new Dictionary<string, object>
            {
                ["name"] = "Bureau",
                ["level_ref"] = "revit_1001",
            });
            kg.Annotate(suite, room, EdgeTypes.Contains,
                new Dictionary<string, object> { ["since"] = "phase1" });

            var (replayed, stats) = ProjectKgReplay.Replay("p1", sink.Entries);

            Assert.Equal(0, stats.Skipped);
            Assert.True(replayed.IsUserType("Suite"));
            Assert.Equal("Suite", replayed.GetNode(suite).NodeType);
            Assert.Equal("A", replayed.GetNode(suite).Attrs["name"]);
            Assert.Single(replayed.OutgoingEdges(suite, EdgeTypes.Contains));
        }
    }
}
