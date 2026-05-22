using System;
using RevitMCPKgCommandSet.Core;
using Xunit;

namespace RevitMCPKgCommandSet.Tests
{
    public class NodeTypeRegistryTests
    {
        [Fact]
        public void IsKnown_returns_true_for_vendor_types()
        {
            Assert.True(NodeTypeRegistry.IsKnown("Wall"));
            Assert.True(NodeTypeRegistry.IsKnown("Level"));
            Assert.True(NodeTypeRegistry.IsKnown("Window"));
            Assert.True(NodeTypeRegistry.IsKnown("Stair"));
            Assert.True(NodeTypeRegistry.IsKnown("DxfImportContext"));
        }

        [Fact]
        public void IsKnown_returns_false_for_unknown()
        {
            Assert.False(NodeTypeRegistry.IsKnown("FooBar"));
            Assert.False(NodeTypeRegistry.IsKnown(""));
        }

        [Fact]
        public void Get_throws_on_unknown()
        {
            Assert.Throws<ArgumentException>(() => NodeTypeRegistry.Get("FooBar"));
        }

        [Fact]
        public void Wall_spec_matches_vendor()
        {
            var spec = NodeTypeRegistry.Get("Wall");
            Assert.Contains("type_ref", spec.Required);
            Assert.Contains("level_ref", spec.Required);
            Assert.Contains("p1", spec.Required);
            Assert.Contains("p2", spec.Required);
            Assert.Contains("length", spec.Required);
            Assert.Contains("height", spec.Required);
            Assert.Empty(spec.Optional);
        }

        [Fact]
        public void WallType_spec_matches_vendor()
        {
            var spec = NodeTypeRegistry.Get("WallType");
            Assert.Contains("name", spec.Required);
            Assert.Contains("total_thickness", spec.Required);
            Assert.Contains("layers_summary", spec.Optional);
        }

        [Fact]
        public void SessionNodeTypes_contains_DxfImportContext_and_Stair()
        {
            Assert.Contains("DxfImportContext", NodeTypeRegistry.SessionNodeTypes);
            Assert.Contains("Stair", NodeTypeRegistry.SessionNodeTypes);
        }

        [Fact]
        public void SessionNodeTypes_excludes_rebuildable_types()
        {
            Assert.DoesNotContain("Wall", NodeTypeRegistry.SessionNodeTypes);
            Assert.DoesNotContain("Level", NodeTypeRegistry.SessionNodeTypes);
            Assert.DoesNotContain("WallType", NodeTypeRegistry.SessionNodeTypes);
        }
    }
}
