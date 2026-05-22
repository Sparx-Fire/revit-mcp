using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RevitMCPKgCommandSet.Models;

namespace RevitMCPKgCommandSet.Core
{
    // Server-side aggregation over already-filtered nodes. Returns a scalar
    // (or per-group table) so the agent does not fetch the whole category
    // and compute count/mean/sum in-context. Single-field only by design
    // (expressions like length*height stay client-side, but field
    // projection keeps that payload small).
    public static class NodeAggregator
    {
        private static readonly HashSet<string> NumericOps =
            new HashSet<string> { "sum", "mean", "min", "max" };

        public static KgAggregateResult Aggregate(
            IEnumerable<Node> nodes, string op, string field, string groupBy)
        {
            op = (op ?? "").Trim().ToLowerInvariant();
            if (op != "count" && !NumericOps.Contains(op))
                throw new ArgumentException(
                    $"unknown aggregate op '{op}' (use count|sum|mean|min|max)");
            if (NumericOps.Contains(op) && string.IsNullOrEmpty(field))
                throw new ArgumentException($"aggregate op '{op}' requires a 'field'");

            var list = nodes.ToList();
            var result = new KgAggregateResult { Op = op, Field = field, GroupBy = groupBy, N = list.Count };

            if (string.IsNullOrEmpty(groupBy))
            {
                result.Value = Compute(list, op, field);
                return result;
            }

            result.Groups = list
                .GroupBy(n => GetAttr(n, groupBy))
                .Select(g => new KgAggGroup
                {
                    Key = g.Key,
                    Value = Compute(g.ToList(), op, field),
                    N = g.Count(),
                })
                .OrderByDescending(x => x.N)
                .ToList();
            return result;
        }

        private static object Compute(List<Node> nodes, string op, string field)
        {
            if (op == "count") return nodes.Count;

            var vals = nodes
                .Select(n => GetNumeric(n, field))
                .Where(v => v.HasValue)
                .Select(v => v.Value)
                .ToList();
            if (vals.Count == 0) return null;

            switch (op)
            {
                case "sum": return vals.Sum();
                case "mean": return vals.Average();
                case "min": return vals.Min();
                case "max": return vals.Max();
                default: throw new ArgumentException($"unknown aggregate op '{op}'");
            }
        }

        private static object GetAttr(Node n, string key)
        {
            if (key == "node_type") return n.NodeType;
            return n.Attrs.TryGetValue(key, out var v) ? v : null;
        }

        private static double? GetNumeric(Node n, string field)
        {
            var v = GetAttr(n, field);
            if (v == null) return null;
            try { return Convert.ToDouble(v, CultureInfo.InvariantCulture); }
            catch { return null; }
        }
    }
}
