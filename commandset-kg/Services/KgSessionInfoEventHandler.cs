using System;
using System.Threading;
using Autodesk.Revit.UI;
using RevitMCPKgCommandSet.Models;
using RevitMCPKgCommandSet.Models.Common;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPKgCommandSet.Services
{
    public class KgSessionInfoEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public AIResult<KgSessionInfoResult> Result { get; private set; }

        public void Execute(UIApplication app)
        {
            try
            {
                KgV2DocumentWatcher.EnsureSubscribed(app);
                var kg = KgV2DocumentWatcher.GetCurrentProjectKg();
                var doc = app?.ActiveUIDocument?.Document;

                var payload = new KgSessionInfoResult
                {
                    ProjectId = kg?.ProjectId ?? string.Empty,
                    DocTitle = KgV2DocumentWatcher.CurrentDocTitle,
                    Turn = kg?.Turn ?? 0,
                    NodeCount = kg?.NodeCount ?? 0,
                    EdgeCount = kg?.EdgeCount ?? 0,
                    PendingDeltaCount = KgV2DocumentWatcher.CurrentPendingCount,
                    EsJournalLength = doc != null ? KgV2DocumentWatcher.ReadEsJournalLength(doc) : 0,
                    LastActionSummary = SummarizeLastAction(kg),
                };

                Result = new AIResult<KgSessionInfoResult>
                {
                    Success = true,
                    Message = "KG v2 session info",
                    Response = payload,
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<KgSessionInfoResult>
                {
                    Success = false,
                    Message = $"kg_session_info failed: {ex.Message}",
                };
            }
            finally
            {
                _resetEvent.Set();
            }
        }

        private static string SummarizeLastAction(RevitMCPKgCommandSet.Core.ProjectKg kg)
        {
            if (kg == null || kg.ActionLog.Count == 0) return "no actions yet";
            var last = kg.ActionLog[kg.ActionLog.Count - 1];
            return $"turn {last.Turn}: {last.Op} {last.TargetId}";
        }

        public bool WaitForCompletion(int timeoutMilliseconds = 10000)
        {
            _resetEvent.Reset();
            return _resetEvent.WaitOne(timeoutMilliseconds);
        }

        public string GetName() => "KG Session Info";
    }
}
