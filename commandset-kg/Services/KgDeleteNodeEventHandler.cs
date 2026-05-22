using System;
using System.Threading;
using Autodesk.Revit.UI;
using RevitMCPKgCommandSet.Core;
using RevitMCPKgCommandSet.Models;
using RevitMCPKgCommandSet.Models.Common;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPKgCommandSet.Services
{
    // Soft-delete a user-defined node (history kept). Refuses Revit-projected
    // nodes: deleting them in the KG would diverge from the model — delete
    // them in Revit instead and let the projection tombstone them.
    public class KgDeleteNodeEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public string LlmId { get; private set; }

        public AIResult<KgUserNodeResult> Result { get; private set; }

        public void SetParameters(string llmId)
        {
            LlmId = llmId;
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
                        Message = "kg_delete_node: no active KG v2 projection",
                    };
                    return;
                }
                if (!kg.HasNode(LlmId))
                {
                    Result = new AIResult<KgUserNodeResult>
                    {
                        Success = false,
                        Message = $"kg_delete_node: no such node {LlmId}",
                    };
                    return;
                }

                var nodeType = kg.GetNode(LlmId).NodeType;
                if (!kg.IsUserType(nodeType))
                {
                    Result = new AIResult<KgUserNodeResult>
                    {
                        Success = false,
                        Message = $"kg_delete_node: {LlmId} is a Revit-projected {nodeType} node; delete it in Revit, not the KG",
                    };
                    return;
                }

                kg.AdvanceTurn();
                kg.SoftDelete(LlmId);
                KgV2DocumentWatcher.FlushCurrent();

                Result = new AIResult<KgUserNodeResult>
                {
                    Success = true,
                    Message = $"KG v2 delete_node {nodeType}: {LlmId}",
                    Response = new KgUserNodeResult
                    {
                        Operation = "delete",
                        LlmId = LlmId,
                        NodeType = nodeType,
                        Turn = kg.Turn,
                    },
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<KgUserNodeResult>
                {
                    Success = false,
                    Message = $"kg_delete_node failed: {ex.Message}",
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

        public string GetName() => "KG Delete Node";
    }
}
