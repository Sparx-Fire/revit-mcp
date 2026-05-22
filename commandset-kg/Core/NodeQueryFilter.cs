using System.Collections.Generic;
using System.Linq;

namespace RevitMCPKgCommandSet.Core
{
    public static class NodeQueryFilter
    {
        public static IEnumerable<Node> Apply(
            ProjectKg kg,
            string nodeType,
            Dictionary<string, object> attrsFilter,
            bool includeSoftDeleted)
        {
            if (kg == null) return Enumerable.Empty<Node>();

            IEnumerable<Node> source = string.IsNullOrEmpty(nodeType)
                ? kg.Nodes
                : kg.NodesOfType(nodeType);

            if (!includeSoftDeleted)
                source = source.Where(n => !n.IsSoftDeleted);

            if (attrsFilter != null && attrsFilter.Count > 0)
                source = source.Where(n => MatchesAttrs(n, attrsFilter));

            return source;
        }

        private static bool MatchesAttrs(Node node, Dictionary<string, object> filter)
        {
            foreach (var kvp in filter)
            {
                if (!node.Attrs.TryGetValue(kvp.Key, out var actual)) return false;
                if (!ValueEquals(actual, kvp.Value)) return false;
            }
            return true;
        }

        private static bool ValueEquals(object a, object b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;
            if (a.GetType() == b.GetType()) return a.Equals(b);

            try
            {
                var aDecimal = System.Convert.ToDouble(a, System.Globalization.CultureInfo.InvariantCulture);
                var bDecimal = System.Convert.ToDouble(b, System.Globalization.CultureInfo.InvariantCulture);
                return aDecimal == bDecimal;
            }
            catch
            {
                return string.Equals(a.ToString(), b.ToString());
            }
        }
    }
}
