using System.Collections.Generic;

namespace RevitMCPKgCommandSet.Core
{
    public interface IElementReader
    {
        string ResolveNodeType(long elementId);

        Dictionary<string, object> ReadAttrs(long elementId);

        IEnumerable<EdgeSpec> ReadEdges(long elementId);

        // Walk every Revit element that maps to a KG node type (Level,
        // WallType, FamilyType, Wall, Window, Door). Used by drift
        // detection to find elements present in the document but absent
        // from the projection.
        IEnumerable<long> EnumerateAllElementIds();
    }

    public enum EdgeDirection
    {
        Outgoing,
        Incoming,
    }

    public sealed class EdgeSpec
    {
        public long PeerElementId { get; }
        public string EdgeType { get; }
        public EdgeDirection Direction { get; }

        public EdgeSpec(long peerElementId, string edgeType, EdgeDirection direction = EdgeDirection.Outgoing)
        {
            PeerElementId = peerElementId;
            EdgeType = edgeType;
            Direction = direction;
        }
    }
}
