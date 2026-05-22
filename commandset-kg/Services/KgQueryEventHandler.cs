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
    public class KgQueryEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public string NodeType { get; private set; }
        public Dictionary<string, object> AttrsFilter { get; private set; }
        public bool IncludeSoftDeleted { get; private set; }
        public HashSet<string> Select { get; private set; }
        public string AggOp { get; private set; }
        public string AggField { get; private set; }
        public string AggGroupBy { get; private set; }
        public List<JoinStep> Join { get; private set; }

        public AIResult<KgQueryResult> Result { get; private set; }

        public void SetParameters(
            string nodeType, Dictionary<string, object> attrsFilter, bool includeSoftDeleted,
            HashSet<string> select = null, string aggOp = null, string aggField = null, string aggGroupBy = null,
            List<JoinStep> join = null)
        {
            NodeType = nodeType;
            AttrsFilter = attrsFilter;
            IncludeSoftDeleted = includeSoftDeleted;
            Select = select;
            AggOp = aggOp;
            AggField = aggField;
            AggGroupBy = aggGroupBy;
            Join = join;
            _resetEvent.Reset();
        }

        public void Execute(UIApplication app)
        {
            try
            {
                KgV2DocumentWatcher.EnsureSubscribed(app);
                var kg = KgV2DocumentWatcher.GetCurrentProjectKg();

                var nodes = NodeQueryFilter.Apply(kg, NodeType, AttrsFilter, IncludeSoftDeleted);

                KgQueryResult response;
                if (!string.IsNullOrEmpty(AggOp))
                {
                    var agg = NodeAggregator.Aggregate(nodes, AggOp, AggField, AggGroupBy);
                    response = new KgQueryResult { Count = agg.N, Nodes = null, Aggregate = agg };
                }
                else if (Join != null && Join.Count > 0)
                {
                    var rows = NodeJoiner.BuildRows(kg, nodes, Select, Join);
                    response = new KgQueryResult { Count = rows.Count, Nodes = null, Rows = rows };
                }
                else
                {
                    var views = NodeViewBuilder.FromMany(nodes, Select);
                    response = new KgQueryResult { Count = views.Count, Nodes = views };
                }

                Result = new AIResult<KgQueryResult>
                {
                    Success = true,
                    Message = "KG v2 query",
                    Response = response,
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<KgQueryResult>
                {
                    Success = false,
                    Message = $"kg_query failed: {ex.Message}",
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

        public string GetName() => "KG Query";
    }
}
