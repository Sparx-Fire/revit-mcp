using System;
using System.Collections.Generic;
using System.Linq;
using RevitMCPKgCommandSet.Core;
using Xunit;

namespace RevitMCPKgCommandSet.Tests
{
    public class ProjectKgEdgeTests
    {
        private static ProjectKg SeededKg()
        {
            var kg = new ProjectKg("p1");
            kg.AddNode("Level", new() { ["name"] = "N0", ["elevation"] = 0.0 }, llmId: "level_001");
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
            return kg;
        }

        [Fact]
        public void AddEdge_known_type_succeeds()
        {
            var kg = SeededKg();
            Assert.True(kg.AddEdge("wall_001", "level_001", EdgeTypes.AtLevel));
            Assert.Equal(1, kg.EdgeCount);
        }

        [Fact]
        public void AddEdge_rejects_unknown_type()
        {
            var kg = SeededKg();
            Assert.Throws<ArgumentException>(() =>
                kg.AddEdge("wall_001", "level_001", "wat"));
        }

        [Fact]
        public void AddEdge_rejects_missing_src_or_dst()
        {
            var kg = SeededKg();
            Assert.Throws<KeyNotFoundException>(() =>
                kg.AddEdge("does_not_exist", "level_001", EdgeTypes.AtLevel));
            Assert.Throws<KeyNotFoundException>(() =>
                kg.AddEdge("wall_001", "does_not_exist", EdgeTypes.AtLevel));
        }

        [Fact]
        public void AddEdge_duplicate_same_type_returns_false()
        {
            var kg = SeededKg();
            Assert.True(kg.AddEdge("wall_001", "level_001", EdgeTypes.AtLevel));
            Assert.False(kg.AddEdge("wall_001", "level_001", EdgeTypes.AtLevel));
            Assert.Equal(1, kg.EdgeCount);
        }

        [Fact]
        public void AddEdge_different_types_between_same_pair_allowed()
        {
            var kg = SeededKg();
            Assert.True(kg.AddEdge("wall_001", "level_001", EdgeTypes.AtLevel));
            Assert.True(kg.AddEdge("wall_001", "level_001", EdgeTypes.DerivedFrom));
            Assert.Equal(2, kg.EdgeCount);
        }

        [Fact]
        public void OutgoingEdges_lists_all_for_src()
        {
            var kg = SeededKg();
            kg.AddEdge("wall_001", "level_001", EdgeTypes.AtLevel);
            kg.AddEdge("wall_001", "walltype_001", EdgeTypes.IsType);
            var outgoing = kg.OutgoingEdges("wall_001").ToList();
            Assert.Equal(2, outgoing.Count);
        }

        [Fact]
        public void OutgoingEdges_filters_by_type()
        {
            var kg = SeededKg();
            kg.AddEdge("wall_001", "level_001", EdgeTypes.AtLevel);
            kg.AddEdge("wall_001", "walltype_001", EdgeTypes.IsType);
            var filtered = kg.OutgoingEdges("wall_001", EdgeTypes.AtLevel).ToList();
            Assert.Single(filtered);
            Assert.Equal("level_001", filtered[0].Dst);
        }

        [Fact]
        public void IncomingEdges_lists_all_for_dst()
        {
            var kg = SeededKg();
            kg.AddEdge("wall_001", "level_001", EdgeTypes.AtLevel);
            var incoming = kg.IncomingEdges("level_001").ToList();
            Assert.Single(incoming);
            Assert.Equal("wall_001", incoming[0].Src);
        }

        [Fact]
        public void RemoveEdge_returns_true_when_present()
        {
            var kg = SeededKg();
            kg.AddEdge("wall_001", "level_001", EdgeTypes.AtLevel);
            Assert.True(kg.RemoveEdge("wall_001", "level_001", EdgeTypes.AtLevel));
            Assert.Equal(0, kg.EdgeCount);
        }

        [Fact]
        public void RemoveEdge_returns_false_when_missing()
        {
            var kg = SeededKg();
            Assert.False(kg.RemoveEdge("wall_001", "level_001", EdgeTypes.AtLevel));
        }

        [Fact]
        public void RemoveEdge_clears_indices()
        {
            var kg = SeededKg();
            kg.AddEdge("wall_001", "level_001", EdgeTypes.AtLevel);
            kg.RemoveEdge("wall_001", "level_001", EdgeTypes.AtLevel);
            Assert.Empty(kg.OutgoingEdges("wall_001"));
            Assert.Empty(kg.IncomingEdges("level_001"));
        }
    }
}
