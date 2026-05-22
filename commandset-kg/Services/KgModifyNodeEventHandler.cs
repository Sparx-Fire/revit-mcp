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
    // Modify attrs of a user-defined node. Refuses Revit-projected nodes:
    // those are owned by the projection and editing them out of band would
    // create drift — change them in Revit instead.
    public class KgModifyNodeEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public string LlmId { get; private set; }
        public Dictionary<string, object> Updates { get; private set; }

        public AIResult<KgUserNodeResult> Result { get; private set; }

        public void SetParameters(string llmId, Dictionary<string, object> updates)
        {
            LlmId = llmId;
            Updates = updates;
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
                        Message = "kg_modify_node: no active KG v2 projection",
                    };
                    return;
                }
                if (!kg.HasNode(LlmId))
                {
                    Result = new AIResult<KgUserNodeResult>
                    {
                        Success = false,
                        Message = $"kg_modify_node: no such node {LlmId}",
                    };
                    return;
                }

                var nodeType = kg.GetNode(LlmId).NodeType;
                if (!kg.IsUserType(nodeType))
                {
                    Result = new AIResult<KgUserNodeResult>
                    {
                        Success = false,
                        Message = $"kg_modify_node: {LlmId} is a Revit-projected {nodeType} node; edit it in Revit, not the KG",
                    };
                    return;
                }

                kg.AdvanceTurn();
                kg.ModifyNode(LlmId, Updates ?? new Dictionary<string, object>());
                KgV2DocumentWatcher.FlushCurrent();

                Result = new AIResult<KgUserNodeResult>
                {
                    Success = true,
                    Message = $"KG v2 modify_node {nodeType}: {LlmId}",
                    Response = new KgUserNodeResult
                    {
                        Operation = "modify",
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
                    Message = $"kg_modify_node failed: {ex.Message}",
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

        public string GetName() => "KG Modify Node";
    }
}
