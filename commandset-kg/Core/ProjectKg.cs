using System;
using System.Collections.Generic;
using System.Linq;

namespace RevitMCPKgCommandSet.Core
{
    public sealed class ProjectKg
    {
        public string ProjectId { get; }

        private readonly Dictionary<string, Node> _nodes = new Dictionary<string, Node>();
        private readonly Dictionary<EdgeKey, Edge> _edges = new Dictionary<EdgeKey, Edge>();
        private readonly Dictionary<string, HashSet<EdgeKey>> _outgoing = new Dictionary<string, HashSet<EdgeKey>>();
        private readonly Dictionary<string, HashSet<EdgeKey>> _incoming = new Dictionary<string, HashSet<EdgeKey>>();
        private readonly List<ActionLogEntry> _actionLog = new List<ActionLogEntry>();
        private readonly Dictionary<string, int> _counters = new Dictionary<string, int>();
        // User-defined semantic node types (e.g. Suite, Zone) authored at
        // runtime. Free-form attrs, no Revit binding. Per-instance so a
        // type declared in one document never leaks into another.
        private readonly HashSet<string> _userTypes = new HashSet<string>();
        // User-defined semantic EDGE types (e.g. adjacent_to) authored at
        // runtime via Annotate, symmetric to _userTypes for nodes. KG-owned
        // like every F2 edge: never emitted/repatched by the Revit projection
        // (so they survive re-projection), and must not collide with an F1
        // (Revit-owned) edge type. Registered on first authoring use.
        private readonly HashSet<string> _userEdgeTypes = new HashSet<string>();

        private int _turn;
        private IDeltaSink _sink;

        public ProjectKg(string projectId)
        {
            if (string.IsNullOrEmpty(projectId))
                throw new ArgumentException("projectId required", nameof(projectId));
            ProjectId = projectId;
        }

        public int Turn => _turn;

        public void AttachSink(IDeltaSink sink) => _sink = sink;
        public void DetachSink() => _sink = null;

        public int AdvanceTurn()
        {
            _turn++;
            _sink?.Emit(new DeltaEntry { Turn = _turn, Op = DeltaOps.AdvanceTurn });
            return _turn;
        }

        public int NodeCount => _nodes.Count;
        public int EdgeCount => _edges.Count;
        public IReadOnlyList<ActionLogEntry> ActionLog => _actionLog;
        public IEnumerable<Node> Nodes => _nodes.Values;
        public IEnumerable<Edge> Edges => _edges.Values;

        public bool HasNode(string llmId) => _nodes.ContainsKey(llmId);

        public Node GetNode(string llmId)
        {
            if (!_nodes.TryGetValue(llmId, out var node))
                throw new KeyNotFoundException(llmId);
            return node;
        }

        public IEnumerable<Node> NodesOfType(string nodeType) =>
            _nodes.Values.Where(n => n.NodeType == nodeType);

        public IEnumerable<Edge> OutgoingEdges(string srcId, string edgeType = null)
        {
            if (!_outgoing.TryGetValue(srcId, out var keys)) yield break;
            foreach (var k in keys)
                if (edgeType == null || k.EdgeType == edgeType) yield return _edges[k];
        }

        public IEnumerable<Edge> IncomingEdges(string dstId, string edgeType = null)
        {
            if (!_incoming.TryGetValue(dstId, out var keys)) yield break;
            foreach (var k in keys)
                if (edgeType == null || k.EdgeType == edgeType) yield return _edges[k];
        }

        // ---- Node ops ----

        public string AddNode(string nodeType, Dictionary<string, object> attrs, string llmId = null, bool emitLog = true)
        {
            var spec = NodeTypeRegistry.Get(nodeType);
            var keys = new HashSet<string>(attrs.Keys);

            var missing = new HashSet<string>(spec.Required);
            missing.ExceptWith(keys);
            if (missing.Count > 0)
                throw new ArgumentException($"Missing required attrs for {nodeType}: {string.Join(",", missing.OrderBy(x => x))}");

            var unknown = new HashSet<string>(keys);
            unknown.ExceptWith(spec.Required);
            unknown.ExceptWith(spec.Optional);
            if (unknown.Count > 0)
                throw new ArgumentException($"Unknown attrs for {nodeType}: {string.Join(",", unknown.OrderBy(x => x))}");

            if (llmId == null)
            {
                llmId = NextLlmId(nodeType);
            }
            else
            {
                // Caller-supplied id (Replay path, restore, etc.): bump the
                // counter so that a later NextLlmId() doesn't collide with
                // a node loaded from a prior session.
                BumpCounterFromLlmId(nodeType, llmId);
            }
            if (_nodes.ContainsKey(llmId))
                throw new ArgumentException($"llm_id already in graph: {llmId}");

            var fullAttrs = new Dictionary<string, object>(attrs)
            {
                [LifecycleAttrs.Type] = nodeType,
                [LifecycleAttrs.CreatedAt] = _turn,
                [LifecycleAttrs.ModifiedAt] = new List<int>(),
                [LifecycleAttrs.DeletedAt] = null,
            };

            _nodes[llmId] = new Node(llmId, nodeType, fullAttrs);

            if (emitLog)
            {
                _actionLog.Add(new ActionLogEntry(_turn, "create", llmId, new Dictionary<string, object>
                {
                    ["node_type"] = nodeType,
                    ["attrs"] = new Dictionary<string, object>(attrs),
                }));
            }

            _sink?.Emit(new DeltaEntry
            {
                Turn = _turn,
                Op = DeltaOps.CreateNode,
                Id = llmId,
                NodeType = nodeType,
                Attrs = new Dictionary<string, object>(attrs),
            });

            return llmId;
        }

        public bool IsUserType(string nodeType) => _userTypes.Contains(nodeType);

        public bool IsUserEdgeType(string edgeType) => _userEdgeTypes.Contains(edgeType);

        public IReadOnlyCollection<string> UserEdgeTypes => _userEdgeTypes;

        // Create a user-authored semantic node (Suite, Zone, ...). Unlike
        // AddNode it has NO schema: any attr keys are accepted and there are
        // no required attrs. The type name must not collide with a built-in
        // (Revit-projected) type — those are owned by the projection and
        // would be ambiguous to query/drift. The node carries no revit_id,
        // so it is invisible to drift detection and never touched by rescan.
        public string AddUserNode(string nodeType, Dictionary<string, object> attrs, string llmId = null, bool emitLog = true)
        {
            if (string.IsNullOrWhiteSpace(nodeType))
                throw new ArgumentException("node_type required");
            if (NodeTypeRegistry.IsKnown(nodeType))
                throw new ArgumentException($"'{nodeType}' is a built-in node type; user types must use a distinct name");
            attrs = attrs ?? new Dictionary<string, object>();

            _userTypes.Add(nodeType);

            if (llmId == null)
            {
                llmId = NextLlmId(nodeType);
            }
            else
            {
                BumpCounterFromLlmId(nodeType, llmId);
            }
            if (_nodes.ContainsKey(llmId))
                throw new ArgumentException($"llm_id already in graph: {llmId}");

            var fullAttrs = new Dictionary<string, object>(attrs)
            {
                [LifecycleAttrs.Type] = nodeType,
                [LifecycleAttrs.CreatedAt] = _turn,
                [LifecycleAttrs.ModifiedAt] = new List<int>(),
                [LifecycleAttrs.DeletedAt] = null,
            };

            _nodes[llmId] = new Node(llmId, nodeType, fullAttrs);

            if (emitLog)
            {
                _actionLog.Add(new ActionLogEntry(_turn, "create", llmId, new Dictionary<string, object>
                {
                    ["node_type"] = nodeType,
                    ["attrs"] = new Dictionary<string, object>(attrs),
                    ["user_defined"] = true,
                }));
            }

            _sink?.Emit(new DeltaEntry
            {
                Turn = _turn,
                Op = DeltaOps.CreateUserNode,
                Id = llmId,
                NodeType = nodeType,
                Attrs = new Dictionary<string, object>(attrs),
            });

            return llmId;
        }

        public void ModifyNode(string llmId, Dictionary<string, object> updates)
        {
            if (!_nodes.TryGetValue(llmId, out var node))
                throw new KeyNotFoundException(llmId);
            if (node.IsSoftDeleted)
                throw new InvalidOperationException($"Node {llmId} is soft-deleted");

            var updateKeys = new HashSet<string>(updates.Keys);
            // User-defined types are free-form: any attr key is allowed.
            // Built-in types validate against their closed schema.
            if (!_userTypes.Contains(node.NodeType))
            {
                var spec = NodeTypeRegistry.Get(node.NodeType);
                var unknown = new HashSet<string>(updateKeys);
                unknown.ExceptWith(spec.Required);
                unknown.ExceptWith(spec.Optional);
                if (unknown.Count > 0)
                    throw new ArgumentException($"Unknown attrs for {node.NodeType}: {string.Join(",", unknown.OrderBy(x => x))}");
            }

            var before = new Dictionary<string, object>();
            foreach (var k in updateKeys)
            {
                before[k] = node.Attrs.TryGetValue(k, out var v) ? v : null;
                node.Attrs[k] = updates[k];
            }

            var modList = new List<int>(node.ModifiedAtTurns) { _turn };
            node.Attrs[LifecycleAttrs.ModifiedAt] = modList;

            _actionLog.Add(new ActionLogEntry(_turn, "modify", llmId, new Dictionary<string, object>
            {
                ["before"] = before,
                ["after"] = new Dictionary<string, object>(updates),
            }));

            _sink?.Emit(new DeltaEntry
            {
                Turn = _turn,
                Op = DeltaOps.ModifyNode,
                Id = llmId,
                Updates = new Dictionary<string, object>(updates),
            });
        }

        public void SoftDelete(string llmId)
        {
            if (!_nodes.TryGetValue(llmId, out var node))
                throw new KeyNotFoundException(llmId);
            if (node.IsSoftDeleted) return;

            node.Attrs[LifecycleAttrs.DeletedAt] = _turn;
            _actionLog.Add(new ActionLogEntry(_turn, "delete", llmId, new Dictionary<string, object>()));

            _sink?.Emit(new DeltaEntry { Turn = _turn, Op = DeltaOps.SoftDelete, Id = llmId });
        }

        // Clear the tombstone on a soft-deleted node, preserving llm_id and
        // history. Called on the projection path when Revit re-creates an
        // element with the SAME ElementId (Ctrl+Z on a delete, or Ctrl+Y on
        // an undone create). No-op if the node is already alive.
        public void Resurrect(string llmId)
        {
            if (!_nodes.TryGetValue(llmId, out var node))
                throw new KeyNotFoundException(llmId);
            if (!node.IsSoftDeleted) return;

            node.Attrs[LifecycleAttrs.DeletedAt] = null;
            _actionLog.Add(new ActionLogEntry(_turn, "resurrect", llmId, new Dictionary<string, object>()));

            _sink?.Emit(new DeltaEntry { Turn = _turn, Op = DeltaOps.Resurrect, Id = llmId });
        }

        public void SetRevitId(string llmId, long revitId)
        {
            if (!_nodes.TryGetValue(llmId, out var node))
                throw new KeyNotFoundException(llmId);
            node.Attrs[LifecycleAttrs.RevitId] = revitId;
            _sink?.Emit(new DeltaEntry { Turn = _turn, Op = DeltaOps.SetRevitId, Id = llmId, RevitId = revitId });
        }

        public string FindByRevitId(long revitId)
        {
            foreach (var kvp in _nodes)
            {
                if (kvp.Value.RevitId == revitId) return kvp.Key;
            }
            return null;
        }

        // ---- Edge ops ----

        public bool AddEdge(string src, string dst, string edgeType, Dictionary<string, object> attrs = null)
        {
            if (!EdgeTypes.All.Contains(edgeType))
                throw new ArgumentException($"Unknown edge type: {edgeType}");
            if (!_nodes.ContainsKey(src)) throw new KeyNotFoundException($"src: {src}");
            if (!_nodes.ContainsKey(dst)) throw new KeyNotFoundException($"dst: {dst}");

            var key = new EdgeKey(src, dst, edgeType);
            if (_edges.ContainsKey(key)) return false;

            _edges[key] = new Edge(src, dst, edgeType, attrs);
            if (!_outgoing.TryGetValue(src, out var outSet)) _outgoing[src] = outSet = new HashSet<EdgeKey>();
            outSet.Add(key);
            if (!_incoming.TryGetValue(dst, out var inSet)) _incoming[dst] = inSet = new HashSet<EdgeKey>();
            inSet.Add(key);

            _sink?.Emit(new DeltaEntry
            {
                Turn = _turn,
                Op = DeltaOps.AddEdge,
                Src = src,
                Dst = dst,
                EdgeType = edgeType,
                Attrs = attrs != null ? new Dictionary<string, object>(attrs) : null,
            });
            return true;
        }

        public bool RemoveEdge(string src, string dst, string edgeType)
        {
            var key = new EdgeKey(src, dst, edgeType);
            if (!_edges.Remove(key)) return false;
            if (_outgoing.TryGetValue(src, out var outSet)) outSet.Remove(key);
            if (_incoming.TryGetValue(dst, out var inSet)) inSet.Remove(key);

            _sink?.Emit(new DeltaEntry
            {
                Turn = _turn,
                Op = DeltaOps.RemoveEdge,
                Src = src,
                Dst = dst,
                EdgeType = edgeType,
            });
            return true;
        }

        // F2 semantic annotation (DESIGN §2.2, L-10). Upserts the edge with
        // `payload` as attrs ; `payload == null` deletes if present, no-op
        // otherwise. Accepts soft-deleted src/dst so the audit trail
        // (replaced_by, etc.) can outlive the tombstoning of its anchors.
        // Emits exactly one DeltaOps.Annotate entry (or none on no-op).
        //
        // `kind` is either a built-in F2 type (replaced_by, tagged, ...) or a
        // USER-DEFINED edge type (symmetric to AddUserNode): any name that is
        // not a Revit-owned F1 type is accepted and registered on first
        // authoring use. F1 types are owned by the projection and rejected.
        public string Annotate(string src, string dst, string kind, Dictionary<string, object> payload)
        {
            if (string.IsNullOrWhiteSpace(kind))
                throw new ArgumentException("edge kind required");
            if (EdgeTypes.F1.Contains(kind))
                throw new ArgumentException($"'{kind}' is a Revit-owned (F1) edge type; it is maintained by the projection and cannot be authored");
            if (!_nodes.ContainsKey(src)) throw new KeyNotFoundException($"src: {src}");
            if (!_nodes.ContainsKey(dst)) throw new KeyNotFoundException($"dst: {dst}");

            // Anything that is neither F1 nor a built-in F2 annotation is a
            // user-defined edge type. Register it when we actually author an
            // edge (upsert path below); a pure delete/no-op of an unknown
            // type registers nothing, mirroring node-type semantics.
            bool isUserEdge = !EdgeTypes.F2.Contains(kind);

            var key = new EdgeKey(src, dst, kind);
            var exists = _edges.TryGetValue(key, out var existing);

            if (payload == null)
            {
                if (!exists) return "noop";
                _edges.Remove(key);
                if (_outgoing.TryGetValue(src, out var outSet)) outSet.Remove(key);
                if (_incoming.TryGetValue(dst, out var inSet)) inSet.Remove(key);

                _actionLog.Add(new ActionLogEntry(_turn, "annotate_delete", $"{src}->{dst}",
                    new Dictionary<string, object> { ["edge_type"] = kind }));
                _sink?.Emit(new DeltaEntry
                {
                    Turn = _turn,
                    Op = DeltaOps.Annotate,
                    Src = src,
                    Dst = dst,
                    EdgeType = kind,
                });
                return "delete";
            }

            if (isUserEdge) _userEdgeTypes.Add(kind);

            var attrsCopy = new Dictionary<string, object>(payload);
            if (exists)
            {
                _edges[key] = new Edge(src, dst, kind, attrsCopy);
            }
            else
            {
                _edges[key] = new Edge(src, dst, kind, attrsCopy);
                if (!_outgoing.TryGetValue(src, out var outSet)) _outgoing[src] = outSet = new HashSet<EdgeKey>();
                outSet.Add(key);
                if (!_incoming.TryGetValue(dst, out var inSet)) _incoming[dst] = inSet = new HashSet<EdgeKey>();
                inSet.Add(key);
            }

            _actionLog.Add(new ActionLogEntry(_turn, "annotate", $"{src}->{dst}", new Dictionary<string, object>
            {
                ["edge_type"] = kind,
                ["payload"] = new Dictionary<string, object>(payload),
                ["user_defined"] = isUserEdge,
            }));
            _sink?.Emit(new DeltaEntry
            {
                Turn = _turn,
                Op = DeltaOps.Annotate,
                Src = src,
                Dst = dst,
                EdgeType = kind,
                Attrs = new Dictionary<string, object>(payload),
            });

            return exists ? "replace" : "upsert";
        }

        // ---- llm_id allocator ----

        private string NextLlmId(string nodeType)
        {
            _counters.TryGetValue(nodeType, out var c);
            c++;
            _counters[nodeType] = c;
            return $"{nodeType.ToLowerInvariant()}_{c:D3}";
        }

        // Sync the per-type counter to the suffix of a caller-supplied
        // llm_id. Called from AddNode when llmId is explicit (Replay path)
        // so that subsequent NextLlmId() calls never produce a colliding id.
        // Pattern recognised: "<lowercase_type>_<NNN>". Free-form ids are
        // accepted but don't move the counter (they wouldn't have collided
        // with the auto-generated pattern anyway).
        private void BumpCounterFromLlmId(string nodeType, string llmId)
        {
            if (string.IsNullOrEmpty(llmId)) return;
            var prefix = nodeType.ToLowerInvariant() + "_";
            if (!llmId.StartsWith(prefix, StringComparison.Ordinal)) return;
            var suffix = llmId.Substring(prefix.Length);
            if (int.TryParse(suffix, out var n))
            {
                _counters.TryGetValue(nodeType, out var c);
                if (n > c) _counters[nodeType] = n;
            }
        }

        // ---- Transaction (snapshot / restore) ----

        public TransactionScope BeginTransaction() => new TransactionScope(this);

        private sealed class Snapshot
        {
            public Dictionary<string, Node> Nodes;
            public Dictionary<EdgeKey, Edge> Edges;
            public Dictionary<string, HashSet<EdgeKey>> Outgoing;
            public Dictionary<string, HashSet<EdgeKey>> Incoming;
            public List<ActionLogEntry> ActionLog;
            public Dictionary<string, int> Counters;
            public HashSet<string> UserTypes;
            public HashSet<string> UserEdgeTypes;
            public int Turn;
        }

        private Snapshot Capture()
        {
            return new Snapshot
            {
                Nodes = _nodes.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Clone()),
                Edges = _edges.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Clone()),
                Outgoing = _outgoing.ToDictionary(kvp => kvp.Key, kvp => new HashSet<EdgeKey>(kvp.Value)),
                Incoming = _incoming.ToDictionary(kvp => kvp.Key, kvp => new HashSet<EdgeKey>(kvp.Value)),
                ActionLog = _actionLog.Select(e => e.Clone()).ToList(),
                Counters = new Dictionary<string, int>(_counters),
                UserTypes = new HashSet<string>(_userTypes),
                UserEdgeTypes = new HashSet<string>(_userEdgeTypes),
                Turn = _turn,
            };
        }

        private void Restore(Snapshot snap)
        {
            _nodes.Clear();
            foreach (var kvp in snap.Nodes) _nodes[kvp.Key] = kvp.Value;
            _edges.Clear();
            foreach (var kvp in snap.Edges) _edges[kvp.Key] = kvp.Value;
            _outgoing.Clear();
            foreach (var kvp in snap.Outgoing) _outgoing[kvp.Key] = kvp.Value;
            _incoming.Clear();
            foreach (var kvp in snap.Incoming) _incoming[kvp.Key] = kvp.Value;
            _actionLog.Clear();
            _actionLog.AddRange(snap.ActionLog);
            _counters.Clear();
            foreach (var kvp in snap.Counters) _counters[kvp.Key] = kvp.Value;
            _userTypes.Clear();
            foreach (var t in snap.UserTypes) _userTypes.Add(t);
            _userEdgeTypes.Clear();
            foreach (var t in snap.UserEdgeTypes) _userEdgeTypes.Add(t);
            _turn = snap.Turn;
        }

        public sealed class TransactionScope : IDisposable
        {
            private readonly ProjectKg _kg;
            private readonly Snapshot _snap;
            private bool _committed;

            internal TransactionScope(ProjectKg kg)
            {
                _kg = kg;
                _snap = kg.Capture();
            }

            public void Commit() => _committed = true;

            public void Dispose()
            {
                if (!_committed) _kg.Restore(_snap);
            }
        }
    }
}
