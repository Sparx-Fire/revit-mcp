using System.Collections.Generic;

namespace RevitMCPKgCommandSet.Core
{
    public sealed class Edge
    {
        public string Src { get; }
        public string Dst { get; }
        public string EdgeType { get; }
        public Dictionary<string, object> Attrs { get; }

        public Edge(string src, string dst, string edgeType, Dictionary<string, object> attrs = null)
        {
            Src = src;
            Dst = dst;
            EdgeType = edgeType;
            Attrs = attrs ?? new Dictionary<string, object>();
        }

        public EdgeKey Key => new EdgeKey(Src, Dst, EdgeType);

        public Edge Clone() => new Edge(Src, Dst, EdgeType, new Dictionary<string, object>(Attrs));
    }

    public readonly struct EdgeKey : System.IEquatable<EdgeKey>
    {
        public string Src { get; }
        public string Dst { get; }
        public string EdgeType { get; }

        public EdgeKey(string src, string dst, string edgeType)
        {
            Src = src;
            Dst = dst;
            EdgeType = edgeType;
        }

        public bool Equals(EdgeKey other) =>
            Src == other.Src && Dst == other.Dst && EdgeType == other.EdgeType;

        public override bool Equals(object obj) => obj is EdgeKey k && Equals(k);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Src?.GetHashCode() ?? 0;
                hash = (hash * 397) ^ (Dst?.GetHashCode() ?? 0);
                hash = (hash * 397) ^ (EdgeType?.GetHashCode() ?? 0);
                return hash;
            }
        }
    }
}
