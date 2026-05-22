using System.Collections.Generic;
using System.Linq;
using RevitMCPKgCommandSet.Core;
using Xunit;

namespace RevitMCPKgCommandSet.Tests
{
    public class QueryAndTraverseTests
    {
        private static ProjectKg SeededKg()
        {
            var kg = new ProjectKg("p1");
            kg.AddNode("Level", new() { ["name"] = "N0", ["elevation"] = 0.0 }, llmId: "lvl0");
            kg.AddNode("Level", new() { ["name"] = "N1", ["elevation"] = 3.0 }, llmId: "lvl1");
            kg.AddNode("WallType", new() { ["name"] = "WT200", ["total_thickness"] = 0.2 }, llmId: "wt");
            kg.AddNode("Wall", new()
            {
                ["type_ref"] = "wt",
                ["level_ref"] = "lvl0",
                ["p1"] = new[] { 0.0, 0.0 },
                ["p2"] = new[] { 5.0, 0.0 },
                ["length"] = 5.0,
                ["height"] = 3.0,
            }, llmId: "wall0");
            kg.AddNode("Wall", new()
            {
                ["type_ref"] = "wt",
                ["level_ref"] = "lvl1",
                ["p1"] = new[] { 0.0, 0.0 },
                ["p2"] = new[] { 4.0, 0.0 },
                ["length"] = 4.0,
                ["height"] = 3.0,
            }, llmId: "wall1");
            kg.AddEdge("wall0", "lvl0", EdgeTypes.AtLevel);
            kg.AddEdge("wall0", "wt", EdgeTypes.IsType);
            kg.AddEdge("wall1", "lvl1", EdgeTypes.AtLevel);
            kg.AddEdge("wall1", "wt", EdgeTypes.IsType);
            return kg;
        }

        // ---- NodeQueryFilter ----

        [Fact]
        public void Filter_by_type_returns_only_that_type()
        {
            var kg = SeededKg();
            var levels = NodeQueryFilter.Apply(kg, "Level", null, false).ToList();
            Assert.Equal(2, levels.Count);
            Assert.All(levels, n => Assert.Equal("Level", n.NodeType));
        }

        [Fact]
        public void Filter_no_type_returns_all_nodes()
        {
            var kg = SeededKg();
            var all = NodeQueryFilter.Apply(kg, null, null, false).ToList();
            Assert.Equal(5, all.Count);
        }

        [Fact]
        public void Filter_attrs_exact_match_string()
        {
            var kg = SeededKg();
            var found = NodeQueryFilter.Apply(
                kg, "Level",
                new Dictionary<string, object> { ["name"] = "N1" },
                false).ToList();
            Assert.Single(found);
            Assert.Equal("lvl1", found[0].LlmId);
        }

        [Fact]
        public void Filter_attrs_numeric_match_across_int_and_double()
        {
            var kg = SeededKg();
            var found = NodeQueryFilter.Apply(
                kg, "Wall",
                new Dictionary<string, object> { ["length"] = 5 },
                false).ToList();
            Assert.Single(found);
            Assert.Equal("wall0", found[0].LlmId);
        }

        [Fact]
        public void Filter_excludes_soft_deleted_by_default()
        {
            var kg = SeededKg();
            kg.SoftDelete("wall0");
            var walls = NodeQueryFilter.Apply(kg, "Wall", null, false).ToList();
            Assert.Single(walls);
            Assert.Equal("wall1", walls[0].LlmId);
        }

        [Fact]
        public void Filter_includes_soft_deleted_when_flagged()
        {
            var kg = SeededKg();
            kg.SoftDelete("wall0");
            var walls = NodeQueryFilter.Apply(kg, "Wall", null, true).ToList();
            Assert.Equal(2, walls.Count);
        }

        // ---- PathTraversal ----

        [Fact]
        public void Walk_no_path_returns_start_only()
        {
            var kg = SeededKg();
            var reached = PathTraversal.Walk(kg, "wall0", null);
            Assert.Single(reached);
            Assert.Contains("wall0", reached);
        }

        [Fact]
        public void Walk_single_out_step()
        {
            var kg = SeededKg();
            var reached = PathTraversal.Walk(kg, "wall0", new[]
            {
                new TraverseStep { EdgeType = EdgeTypes.AtLevel, Direction = EdgeDirection.Outgoing }
            });
            Assert.Single(reached);
            Assert.Contains("lvl0", reached);
        }

        [Fact]
        public void Walk_single_in_step_reverses_edge()
        {
            var kg = SeededKg();
            var reached = PathTraversal.Walk(kg, "lvl0", new[]
            {
                new TraverseStep { EdgeType = EdgeTypes.AtLevel, Direction = EdgeDirection.Incoming }
            });
            Assert.Single(reached);
            Assert.Contains("wall0", reached);
        }

        [Fact]
        public void Walk_two_steps_fans_then_narrows()
        {
            var kg = SeededKg();
            // wall on lvl0 → all walls at_level → lvl0 (just wall0)
            var reached = PathTraversal.Walk(kg, "wall0", new[]
            {
                new TraverseStep { EdgeType = EdgeTypes.AtLevel, Direction = EdgeDirection.Outgoing },
                new TraverseStep { EdgeType = EdgeTypes.AtLevel, Direction = EdgeDirection.Incoming }
            });
            Assert.Single(reached);
            Assert.Contains("wall0", reached);
        }

        [Fact]
        public void Walk_returns_empty_on_unknown_start()
        {
            var kg = SeededKg();
            var reached = PathTraversal.Walk(kg, "does_not_exist", new[]
            {
                new TraverseStep { EdgeType = EdgeTypes.AtLevel, Direction = EdgeDirection.Outgoing }
            });
            Assert.Empty(reached);
        }
    }
}
