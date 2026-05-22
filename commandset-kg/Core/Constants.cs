using System.Collections.Generic;

namespace RevitMCPKgCommandSet.Core
{
    public static class LifecycleAttrs
    {
        public const string Type = "_type";
        public const string CreatedAt = "created_at_turn";
        public const string ModifiedAt = "modified_at_turn";
        public const string DeletedAt = "deleted_at_turn";
        public const string RevitId = "_revit_id";
        public const string Origin = "_origin";

        public static readonly HashSet<string> Reserved = new HashSet<string>
        {
            Type, CreatedAt, ModifiedAt, DeletedAt, RevitId, Origin,
        };
    }
}
