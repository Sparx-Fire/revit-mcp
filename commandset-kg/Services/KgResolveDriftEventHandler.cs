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
    public class KgResolveDriftEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public string NodeTypeFilter { get; private set; }
        public List<string> Kinds { get; private set; }
        public bool DryRun { get; private set; }

        public AIResult<KgResolveDriftResult> Result { get; private set; }

        public void SetParameters(string nodeTypeFilter, List<string> kinds, bool dryRun)
        {
            NodeTypeFilter = nodeTypeFilter;
            Kinds = kinds;
            DryRun = dryRun;
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
                    Result = new AIResult<KgResolveDriftResult>
                    {
                        Success = false,
                        Message = "kg_resolve_drift: no active KG v2 projection",
                    };
                    return;
                }

                var reader = new RevitElementReader(doc);
                var drift = DriftDetection.Detect(kg, reader, NodeTypeFilter);

                var kindsFilter = ParseKinds(Kinds);

                if (DryRun)
                {
                    // Same shape as a real run, but no mutations. TotalResolved=0,
                    // every entry is in unresolved with reason "dry_run".
                    Result = new AIResult<KgResolveDriftResult>
                    {
                        Success = true,
                        Message = $"KG v2 dry-run: {drift.DriftCount} drift entries",
                        Response = new KgResolveDriftResult
                        {
                            DryRun = true,
                            TotalDetected = drift.DriftCount,
                            Unresolved = drift.Entries
                                .Where(e => kindsFilter == null || kindsFilter.Contains(e.Kind))
                                .Select(e => new KgResolveDriftSkip
                                {
                                    Entry = MapEntry(e),
                                    Reason = "dry_run",
                                })
                                .ToList(),
                        },
                    };
                    return;
                }

                var report = DriftResolution.Resolve(kg, reader, drift, kindsFilter);
                KgV2DocumentWatcher.FlushCurrent();

                Result = new AIResult<KgResolveDriftResult>
                {
                    Success = true,
                    Message = $"KG v2 resolve: {report.TotalResolved}/{report.TotalDetected} aligned",
                    Response = MapReport(report, dryRun: false),
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<KgResolveDriftResult>
                {
                    Success = false,
                    Message = $"kg_resolve_drift failed: {ex.Message}",
                };
            }
            finally
            {
                _resetEvent.Set();
            }
        }

        private static ISet<DriftKind> ParseKinds(List<string> kinds)
        {
            if (kinds == null || kinds.Count == 0) return null;
            var set = new HashSet<DriftKind>();
            foreach (var k in kinds)
            {
                switch ((k ?? string.Empty).ToLowerInvariant())
                {
                    case "missing_in_kg": set.Add(DriftKind.MissingInKg); break;
                    case "orphan_kg_node": set.Add(DriftKind.OrphanKgNode); break;
                    case "tombstoned_but_live": set.Add(DriftKind.TombstonedButLive); break;
                    case "attrs_diverged": set.Add(DriftKind.AttrsDiverged); break;
                }
            }
            return set.Count == 0 ? null : set;
        }

        private static KgResolveDriftResult MapReport(DriftResolutionReport r, bool dryRun) =>
            new KgResolveDriftResult
            {
                DryRun = dryRun,
                TotalDetected = r.TotalDetected,
                TotalResolved = r.TotalResolved,
                ResolvedMissingInKg = r.ResolvedMissingInKg,
                ResolvedAttrsDiverged = r.ResolvedAttrsDiverged,
                ResolvedOrphanKgNode = r.ResolvedOrphanKgNode,
                ResolvedTombstonedButLive = r.ResolvedTombstonedButLive,
                Unresolved = (r.Unresolved ?? new List<DriftResolutionSkip>())
                    .Select(s => new KgResolveDriftSkip
                    {
                        Entry = MapEntry(s.Entry),
                        Reason = s.Reason,
                    })
                    .ToList(),
            };

        private static KgDetectDriftEntry MapEntry(DriftEntry e)
        {
            if (e == null) return null;
            return new KgDetectDriftEntry
            {
                RevitId = e.RevitId,
                LlmId = e.LlmId,
                NodeType = e.NodeType,
                Kind = KindToWire(e.Kind),
                KgAttrs = e.KgAttrs,
                RevitAttrs = e.RevitAttrs,
            };
        }

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

        public string GetName() => "KG Resolve Drift";
    }
}
