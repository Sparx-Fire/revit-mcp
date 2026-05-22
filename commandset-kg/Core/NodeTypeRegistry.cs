using System.Collections.Generic;

namespace RevitMCPKgCommandSet.Core
{
    public sealed class NodeTypeSpec
    {
        public HashSet<string> Required { get; }
        public HashSet<string> Optional { get; }
        public bool RebuiltByRescan { get; }

        public NodeTypeSpec(IEnumerable<string> required, IEnumerable<string> optional = null, bool rebuiltByRescan = true)
        {
            Required = new HashSet<string>(required);
            Optional = new HashSet<string>(optional ?? new string[0]);
            RebuiltByRescan = rebuiltByRescan;
        }
    }

    public static class NodeTypeRegistry
    {
        public static readonly IReadOnlyDictionary<string, NodeTypeSpec> Types = new Dictionary<string, NodeTypeSpec>
        {
            ["Level"] = new NodeTypeSpec(new[] { "name", "elevation" }),
            ["Wall"] = new NodeTypeSpec(new[] { "type_ref", "level_ref", "p1", "p2", "length", "height" }),
            ["Door"] = new NodeTypeSpec(new[] { "type_ref", "host_wall_ref", "position", "sill_height", "head_height" }),
            ["Window"] = new NodeTypeSpec(new[] { "type_ref", "host_wall_ref", "position", "sill_height", "head_height" }),
            ["Room"] = new NodeTypeSpec(
                new[] { "name", "level_ref" },
                new[] { "area", "boundary_walls", "use_subcategory" }),
            ["WallType"] = new NodeTypeSpec(
                new[] { "name", "total_thickness" },
                new[] { "layers_summary" }),
            ["Floor"] = new NodeTypeSpec(
                new[] { "type_ref", "level_ref", "boundary", "area_m2" },
                new[] { "holes" }),
            ["FloorType"] = new NodeTypeSpec(
                new[] { "name", "total_thickness" },
                new[] { "layers_summary" }),
            ["DxfImportContext"] = new NodeTypeSpec(
                new[] { "directory" },
                new[] { "source", "files", "section_lines", "level_reconciliation", "linked_views" },
                rebuiltByRescan: false),
            ["Column"] = new NodeTypeSpec(new[] { "level_ref", "type_ref", "position", "height", "kind" }),
            ["ColumnType"] = new NodeTypeSpec(new[] { "family_name", "type_name", "kind" }),
            ["FamilyType"] = new NodeTypeSpec(
                new[] { "family_name", "type_name", "category" },
                new[] { "dimensions" }),
            ["Stair"] = new NodeTypeSpec(
                new[] { "footprint", "level_from_ref", "level_to_ref", "n_treads_estimated", "riser_height_mm" },
                new[] { "run_width_m", "direction", "shape", "source_dxf_plan", "source_dxf_section",
                        "hosted_in_hole", "detection_confidence", "runs", "landings", "stairs_type_ref" },
                rebuiltByRescan: false),
            ["StairsType"] = new NodeTypeSpec(new[] { "name", "tread_depth_m", "riser_height_m" }),
            ["ModelLine"] = new NodeTypeSpec(new[] { "p1", "p2", "length" }),
            ["DetailLine"] = new NodeTypeSpec(new[] { "p1", "p2", "length" }),
        };

        public static readonly IReadOnlyCollection<string> SessionNodeTypes = BuildSessionTypes();

        private static IReadOnlyCollection<string> BuildSessionTypes()
        {
            var set = new HashSet<string>();
            foreach (var kvp in Types)
                if (!kvp.Value.RebuiltByRescan) set.Add(kvp.Key);
            return set;
        }

        public static bool IsKnown(string type) => Types.ContainsKey(type);

        public static NodeTypeSpec Get(string type)
        {
            if (!Types.TryGetValue(type, out var spec))
                throw new System.ArgumentException($"Unknown node type: {type}");
            return spec;
        }
    }
}
