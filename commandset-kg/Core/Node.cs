using System.Collections.Generic;

namespace RevitMCPKgCommandSet.Core
{
    public sealed class Node
    {
        public string LlmId { get; }
        public string NodeType { get; }
        public Dictionary<string, object> Attrs { get; }

        public Node(string llmId, string nodeType, Dictionary<string, object> attrs)
        {
            LlmId = llmId;
            NodeType = nodeType;
            Attrs = attrs;
        }

        public int CreatedAtTurn => (int)Attrs[LifecycleAttrs.CreatedAt];

        public List<int> ModifiedAtTurns
        {
            get
            {
                if (!Attrs.TryGetValue(LifecycleAttrs.ModifiedAt, out var v) || v == null)
                    return new List<int>();
                return v is List<int> list ? list : new List<int>();
            }
        }

        public int? DeletedAtTurn
        {
            get
            {
                if (!Attrs.TryGetValue(LifecycleAttrs.DeletedAt, out var v) || v == null) return null;
                return (int)v;
            }
        }

        public bool IsSoftDeleted => DeletedAtTurn != null;

        public long? RevitId
        {
            get
            {
                if (!Attrs.TryGetValue(LifecycleAttrs.RevitId, out var v) || v == null) return null;
                return System.Convert.ToInt64(v);
            }
        }

        public Node Clone()
        {
            return new Node(LlmId, NodeType, new Dictionary<string, object>(Attrs));
        }
    }
}
