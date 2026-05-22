using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPKgCommandSet.Core;
using RevitMCPKgCommandSet.Services;
using RevitMCPSDK.API.Base;

namespace RevitMCPKgCommandSet.Commands
{
    public class KgTraverseCommand : ExternalEventCommandBase
    {
        private KgTraverseEventHandler _handler => (KgTraverseEventHandler)Handler;

        public override string CommandName => "kg_traverse";

        public KgTraverseCommand(UIApplication uiApp)
            : base(new KgTraverseEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                var startId = parameters?["start_id"]?.ToString();
                if (string.IsNullOrEmpty(startId))
                    throw new ArgumentException("start_id required");

                // Reachability (variable-depth BFS) mode: triggered by
                // `edge_types` and/or `max_depth`. Distinct from the fixed
                // `path` mode below.
                var edgeTypesTok = parameters?["edge_types"] as JArray;
                var maxDepthTok = parameters?["max_depth"];
                if (edgeTypesTok != null || maxDepthTok != null)
                {
                    var edgeTypes = edgeTypesTok == null
                        ? new HashSet<string>()
                        : new HashSet<string>(edgeTypesTok.Select(x => x.ToString()));
                    int maxDepth = maxDepthTok?.Value<int>() ?? 8;
                    var dirStr = (parameters?["direction"]?.ToString() ?? "any").ToLowerInvariant();
                    EdgeDirection? dir =
                        (dirStr == "out" || dirStr == "outgoing") ? EdgeDirection.Outgoing :
                        (dirStr == "in" || dirStr == "incoming") ? EdgeDirection.Incoming :
                        (EdgeDirection?)null; // "any" / "both"
                    bool includeSoftDeleted = parameters?["include_soft_deleted"]?.Value<bool>() ?? false;

                    _handler.SetReachabilityParameters(startId, edgeTypes, dir, maxDepth, includeSoftDeleted);

                    if (RaiseAndWaitForCompletion(15000))
                        return _handler.Result;
                    throw new TimeoutException("kg_traverse (reachable) timed out");
                }

                var path = new List<TraverseStep>();
                if (parameters?["path"] is JArray arr)
                {
                    foreach (var step in arr)
                    {
                        var edgeType = step?["edge_type"]?.ToString();
                        var dirStr2 = (step?["direction"]?.ToString() ?? "out").ToLowerInvariant();
                        var dir2 = (dirStr2 == "in" || dirStr2 == "incoming")
                            ? EdgeDirection.Incoming
                            : EdgeDirection.Outgoing;
                        path.Add(new TraverseStep { EdgeType = edgeType, Direction = dir2 });
                    }
                }

                _handler.SetParameters(startId, path);

                if (RaiseAndWaitForCompletion(10000))
                    return _handler.Result;

                throw new TimeoutException("kg_traverse timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"kg_traverse failed: {ex.Message}");
            }
        }
    }
}
