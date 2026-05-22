using System;
using System.Collections.Generic;
using System.Threading;
using Autodesk.Revit.UI;
using RevitMCPKgCommandSet.Core;
using RevitMCPKgCommandSet.Models;
using RevitMCPKgCommandSet.Models.Common;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPKgCommandSet.Services
{
    public class KgTraverseEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public string StartId { get; private set; }
        public List<TraverseStep> Path { get; private set; }

        // Reachability (variable-depth) mode — set when EdgeTypes/MaxDepth given.
        public HashSet<string> EdgeTypes { get; private set; }
        public EdgeDirection? Direction { get; private set; }
        public int MaxDepth { get; private set; }
        public bool IncludeSoftDeleted { get; private set; }
        public bool Reachability { get; private set; }

        public AIResult<KgTraverseResult> Result { get; private set; }

        public void SetParameters(string startId, List<TraverseStep> path)
        {
            StartId = startId;
            Path = path ?? new List<TraverseStep>();
            Reachability = false;
            _resetEvent.Reset();
        }

        public void SetReachabilityParameters(
            string startId, HashSet<string> edgeTypes, EdgeDirection? direction,
            int maxDepth, bool includeSoftDeleted)
        {
            StartId = startId;
            EdgeTypes = edgeTypes;
            Direction = direction;
            MaxDepth = maxDepth <= 0 ? 8 : System.Math.Min(maxDepth, 64);
            IncludeSoftDeleted = includeSoftDeleted;
            Reachability = true;
            _resetEvent.Reset();
        }

        public void Execute(UIApplication app)
        {
            try
            {
                KgV2DocumentWatcher.EnsureSubscribed(app);
                var kg = KgV2DocumentWatcher.GetCurrentProjectKg();

                var reachedList = new List<KgTraverseReachedNode>();

                if (Reachability)
                {
                    var hits = PathTraversal.Reachable(
                        kg, StartId, EdgeTypes, Direction, MaxDepth, IncludeSoftDeleted);
                    foreach (var h in hits)
                    {
                        var n = kg.GetNode(h.LlmId);
                        reachedList.Add(new KgTraverseReachedNode
                        {
                            LlmId = n.LlmId,
                            NodeType = n.NodeType,
                            Depth = h.Depth,
                            SoftDeleted = n.IsSoftDeleted,
                        });
                    }

                    Result = new AIResult<KgTraverseResult>
                    {
                        Success = true,
                        Message = "KG v2 traverse (reachable)",
                        Response = new KgTraverseResult
                        {
                            StartId = StartId,
                            Mode = "reachable",
                            MaxDepth = MaxDepth,
                            Count = reachedList.Count,
                            Reached = reachedList,
                        },
                    };
                    return;
                }

                var reached = PathTraversal.Walk(kg, StartId, Path);
                if (kg != null)
                {
                    foreach (var id in reached)
                    {
                        if (kg.HasNode(id))
                        {
                            var n = kg.GetNode(id);
                            reachedList.Add(new KgTraverseReachedNode
                            {
                                LlmId = n.LlmId,
                                NodeType = n.NodeType,
                            });
                        }
                    }
                }

                Result = new AIResult<KgTraverseResult>
                {
                    Success = true,
                    Message = "KG v2 traverse",
                    Response = new KgTraverseResult
                    {
                        StartId = StartId,
                        Mode = "path",
                        StepCount = Path.Count,
                        Count = reachedList.Count,
                        Reached = reachedList,
                    },
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<KgTraverseResult>
                {
                    Success = false,
                    Message = $"kg_traverse failed: {ex.Message}",
                };
            }
            finally
            {
                _resetEvent.Set();
            }
        }

        public bool WaitForCompletion(int timeoutMilliseconds = 10000)
        {
            _resetEvent.Reset();
            return _resetEvent.WaitOne(timeoutMilliseconds);
        }

        public string GetName() => "KG Traverse";
    }
}
