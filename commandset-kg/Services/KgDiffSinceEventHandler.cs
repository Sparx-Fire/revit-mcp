using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Autodesk.Revit.UI;
using RevitMCPKgCommandSet.Models;
using RevitMCPKgCommandSet.Models.Common;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPKgCommandSet.Services
{
    public class KgDiffSinceEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public int SinceTurn { get; private set; }

        public AIResult<KgDiffSinceResult> Result { get; private set; }

        public void SetParameters(int sinceTurn)
        {
            SinceTurn = sinceTurn;
            _resetEvent.Reset();
        }

        public void Execute(UIApplication app)
        {
            try
            {
                KgV2DocumentWatcher.EnsureSubscribed(app);
                var kg = KgV2DocumentWatcher.GetCurrentProjectKg();

                var entries = (kg?.ActionLog ?? new List<RevitMCPKgCommandSet.Core.ActionLogEntry>())
                    .Where(e => e.Turn > SinceTurn)
                    .Select(e => new KgActionLogView
                    {
                        Turn = e.Turn,
                        Op = e.Op,
                        TargetId = e.TargetId,
                        Payload = new Dictionary<string, object>(e.Payload),
                    })
                    .ToList();

                Result = new AIResult<KgDiffSinceResult>
                {
                    Success = true,
                    Message = "KG v2 diff since",
                    Response = new KgDiffSinceResult
                    {
                        SinceTurn = SinceTurn,
                        CurrentTurn = kg?.Turn ?? 0,
                        Count = entries.Count,
                        Entries = entries,
                    },
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<KgDiffSinceResult>
                {
                    Success = false,
                    Message = $"kg_diff_since failed: {ex.Message}",
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

        public string GetName() => "KG Diff Since";
    }
}
