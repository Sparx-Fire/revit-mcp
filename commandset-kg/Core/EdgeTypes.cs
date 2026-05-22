using System.Collections.Generic;

namespace RevitMCPKgCommandSet.Core
{
    public static class EdgeTypes
    {
        // F1 — derived from Revit by the DocumentChanged projection. Owned
        // by the model: created/modified/deleted in lockstep with Revit
        // elements. Repatched on each modify, cascade-deleted with src.
        public const string AtLevel = "at_level";
        public const string IsType = "is_type";
        public const string Hosts = "hosts";
        public const string BoundedBy = "bounded_by";
        public const string ConnectsAt = "connects_at";
        public const string DerivedFrom = "derived_from";

        // F2 — semantic annotations authored via kg_annotate (DESIGN §2.2,
        // L-10). Owned by the KG: untouched by Revit's DocumentChanged and
        // never repatched. Survive soft-delete + undo/redo so the audit
        // trail (replaced_by, etc.) outlives the tombstoning of its anchor.
        public const string ReplacedBy = "replaced_by";
        public const string Tagged = "tagged";
        public const string ViolatesRule = "violates_rule";
        public const string ImplementsIntent = "implements_intent";
        // Membership: a user-defined semantic node (e.g. Suite, Zone)
        // groups existing nodes (Rooms, Walls, ...). KG-owned like every
        // F2 edge — never repatched by projection, survives undo/redo.
        public const string Contains = "contains";

        public static readonly HashSet<string> F1 = new HashSet<string>
        {
            AtLevel, IsType, Hosts, BoundedBy, ConnectsAt, DerivedFrom,
        };

        public static readonly HashSet<string> F2 = new HashSet<string>
        {
            ReplacedBy, Tagged, ViolatesRule, ImplementsIntent, Contains,
        };

        public static readonly HashSet<string> All = new HashSet<string>
        {
            AtLevel, IsType, Hosts, BoundedBy, ConnectsAt, DerivedFrom,
            ReplacedBy, Tagged, ViolatesRule, ImplementsIntent, Contains,
        };
    }
}
