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
    public class KgQueryCommand : ExternalEventCommandBase
    {
        private KgQueryEventHandler _handler => (KgQueryEventHandler)Handler;

        public override string CommandName => "kg_query";

        public KgQueryCommand(UIApplication uiApp)
            : base(new KgQueryEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                string nodeType = parameters?["node_type"]?.ToString();
                bool includeSoftDeleted = parameters?["include_soft_deleted"]?.Value<bool>() ?? false;

                Dictionary<string, object> attrsFilter = null;
                var f = parameters?["attrs_filter"];
                if (f is JObject jo) attrsFilter = jo.ToObject<Dictionary<string, object>>();

                // (a) field projection
                HashSet<string> select = null;
                if (parameters?["select"] is JArray sa)
                    select = new HashSet<string>(sa.Select(x => x.ToString()));

                // (c) server-side aggregation
                string aggOp = null, aggField = null, aggGroupBy = null;
                if (parameters?["aggregate"] is JObject agg)
                {
                    aggOp = agg["op"]?.ToString();
                    aggField = agg["field"]?.ToString();
                    aggGroupBy = agg["group_by"]?.ToString();
                }

                // edge-aware join projection
                List<JoinStep> join = null;
                if (parameters?["join"] is JArray ja)
                {
                    join = new List<JoinStep>();
                    foreach (var j in ja.OfType<JObject>())
                    {
                        var dirStr = j["direction"]?.ToString()?.Trim().ToLowerInvariant();
                        join.Add(new JoinStep
                        {
                            EdgeType = j["edge_type"]?.ToString(),
                            Direction = dirStr == "incoming" ? EdgeDirection.Incoming : EdgeDirection.Outgoing,
                            As = j["as"]?.ToString(),
                            Select = (j["select"] as JArray)?.Select(x => x.ToString()).ToList(),
                        });
                    }
                }

                _handler.SetParameters(nodeType, attrsFilter, includeSoftDeleted,
                    select, aggOp, aggField, aggGroupBy, join);

                if (RaiseAndWaitForCompletion(10000))
                    return _handler.Result;

                throw new TimeoutException("kg_query timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"kg_query failed: {ex.Message}");
            }
        }
    }
}
