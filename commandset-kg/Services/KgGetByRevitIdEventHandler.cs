using System;
using System.Threading;
using Autodesk.Revit.UI;
using RevitMCPKgCommandSet.Models;
using RevitMCPKgCommandSet.Models.Common;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPKgCommandSet.Services
{
    public class KgGetByRevitIdEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public long RevitId { get; private set; }

        public AIResult<KgGetByRevitIdResult> Result { get; private set; }

        public void SetParameters(long revitId)
        {
            RevitId = revitId;
            _resetEvent.Reset();
        }

        public void Execute(UIApplication app)
        {
            try
            {
                KgV2DocumentWatcher.EnsureSubscribed(app);
                var kg = KgV2DocumentWatcher.GetCurrentProjectKg();

                KgNodeView view = null;
                if (kg != null)
                {
                    var llmId = kg.FindByRevitId(RevitId);
                    if (llmId != null)
                    {
                        var node = kg.GetNode(llmId);
                        view = NodeViewBuilder.From(node);
                    }
                }

                Result = new AIResult<KgGetByRevitIdResult>
                {
                    Success = true,
                    Message = view != null ? "KG v2 get by revit id" : "KG v2 — not found",
                    Response = new KgGetByRevitIdResult { Found = view != null, Node = view },
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<KgGetByRevitIdResult>
                {
                    Success = false,
                    Message = $"kg_get_by_revit_id failed: {ex.Message}",
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

        public string GetName() => "KG Get By Revit Id";
    }
}
