using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Autodesk.Revit.UI;
using RevitMCPKgCommandSet.Core;
using RevitMCPKgCommandSet.Models;
using RevitMCPKgCommandSet.Models.Common;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPKgCommandSet.Services
{
    public class KgDetectDriftEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public string NodeTypeFilter { get; private set; }
        public AIResult<KgDetectDriftResult> Result { get; private set; }

        public void SetParameters(string nodeTypeFilter)
        {
            NodeTypeFilter = nodeTypeFilter;
            _resetEvent.Reset();
        }

        public void Execute(UIApplication app)
        {
            try
            {
                KgV2DocumentWatcher.EnsureSubscribed(app);
                var kg = KgV2DocumentWatcher.GetCurrentProjectKg();
                var doc = app?.ActiveUIDocument?.Document;

                if (kg == null || doc == null)
                {
                    Result = new AIResult<KgDetectDriftResult>
                    {
                        Success = false,
                        Message = "kg_detect_drift: no active KG v2 projection",
                    };
                    return;
                }

                var reader = new RevitElementReader(doc);
                var report = DriftDetection.Detect(kg, reader, NodeTypeFilter);

                Result = new AIResult<KgDetectDriftResult>
                {
                    Success = true,
                    Message = $"KG v2 drift: {report.DriftCount}/{report.TotalChecked}",
                    Response = new KgDetectDriftResult
                    {
                        TotalChecked = report.TotalChecked,
                        DriftCount = report.DriftCount,
                        Entries = report.Entries.Select(MapEntry).ToList(),
                    },
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<KgDetectDriftResult>
                {
                    Success = false,
                    Message = $"kg_detect_drift failed: {ex.Message}",
                };
            }
            finally
            {
                _resetEvent.Set();
            }
        }

        private static KgDetectDriftEntry MapEntry(DriftEntry e) =>
            new KgDetectDriftEntry
            {
                RevitId = e.RevitId,
                LlmId = e.LlmId,
                NodeType = e.NodeType,
                Kind = KindToWire(e.Kind),
                KgAttrs = e.KgAttrs,
                RevitAttrs = e.RevitAttrs,
            };

        private static string KindToWire(DriftKind k)
        {
            switch (k)
            {
                case DriftKind.MissingInKg: return "missing_in_kg";
                case DriftKind.OrphanKgNode: return "orphan_kg_node";
                case DriftKind.TombstonedButLive: return "tombstoned_but_live";
                case DriftKind.AttrsDiverged: return "attrs_diverged";
                default: return "unknown";
            }
        }

        public bool WaitForCompletion(int timeoutMilliseconds = 10000)
        {
            _resetEvent.Reset();
            return _resetEvent.WaitOne(timeoutMilliseconds);
        }

        public string GetName() => "KG Detect Drift";
    }
}
