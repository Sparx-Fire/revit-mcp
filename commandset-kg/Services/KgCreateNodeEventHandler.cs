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
    // Author a user-defined semantic node (e.g. Suite, Zone). Free-form
    // attrs, no Revit binding. The type name must not collide with a
    // built-in (Revit-projected) type.
    public class KgCreateNodeEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public string NodeType { get; private set; }
        public Dictionary<string, object> Attrs { get; private set; }

        public AIResult<KgUserNodeResult> Result { get; private set; }

        public void SetParameters(string nodeType, Dictionary<string, object> attrs)
        {
            NodeType = nodeType;
            Attrs = attrs;
            _resetEvent.Reset();
        }

        public void Execute(UIApplication app)
        {
            try
            {
                KgV2DocumentWatcher.EnsureSubscribed(app);
                var kg = KgV2DocumentWatcher.GetCurrentProjectKg();
                if (kg == null)
                {
                    Result = new AIResult<KgUserNodeResult>
                    {
                        Success = false,
                        Message = "kg_create_node: no active KG v2 projection",
                    };
                    return;
                }

                // Land the node on a fresh turn so created_at_turn > 0 marks
                // it as user-authored (distinct from turn-0 bootstrap nodes).
                kg.AdvanceTurn();
                var llmId = kg.AddUserNode(NodeType, Attrs);
                KgV2DocumentWatcher.FlushCurrent();

                Result = new AIResult<KgUserNodeResult>
                {
                    Success = true,
                    Message = $"KG v2 create_node {NodeType}: {llmId}",
                    Response = new KgUserNodeResult
                    {
                        Operation = "create",
                        LlmId = llmId,
                        NodeType = NodeType,
                        Turn = kg.Turn,
                    },
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<KgUserNodeResult>
                {
                    Success = false,
                    Message = $"kg_create_node failed: {ex.Message}",
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

        public string GetName() => "KG Create Node";
    }
}
