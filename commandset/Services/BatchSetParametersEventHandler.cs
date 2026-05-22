using System;
using System.Collections.Generic;
using System.Threading;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPCommandSet.Models.Common;
using RevitMCPCommandSet.Utils;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services
{
    /// <summary>
    /// Apply a list of {element_id, param, value} mutations within a SINGLE
    /// Revit Transaction. O(N) per call where N is the batch size, vs the
    /// upstream pattern of one Transaction per modify which incurs ~100-
    /// 300ms Revit overhead + one DocumentChanged event each.
    ///
    /// Use case — Stage-3 80_m-long (30 wall height edits) baselines at
    /// ~$2.09 with N Transactions ; expected ~$0.3-0.6 with this single
    /// batched Transaction.
    ///
    /// Failure handling :
    ///   - SwallowWarningsPreprocessor on the Transaction (no modal
    ///     dialogs hang MCP headless).
    ///   - Per-op exceptions captured into Errors[]. If atomic=true (default)
    ///     any failure rolls back the whole txn; if false, partial commit.
    /// </summary>
    public sealed class BatchSetParametersEventHandler
        : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public BatchSetParametersSetting Setting { get; private set; }
        public AIResult<BatchSetParametersResult> Result { get; private set; }

        public void SetParameters(BatchSetParametersSetting setting)
        {
            Setting = setting;
            Result = null;
            _resetEvent.Reset();
        }

        public void Execute(UIApplication app)
        {
            try
            {
                var doc = app?.ActiveUIDocument?.Document;
                if (doc == null) throw new InvalidOperationException("no active document");
                if (Setting == null) throw new ArgumentNullException(nameof(Setting));

                var ops = Setting.Operations ?? new List<BatchSetParameterOperation>();
                var payload = new BatchSetParametersResult { Total = ops.Count };

                using var tx = new Transaction(doc, "batch_set_parameters");
                tx.StartWithSwallowedWarnings();

                for (int i = 0; i < ops.Count; i++)
                {
                    var op = ops[i];
                    try
                    {
                        ApplyOne(doc, op);
                        payload.Succeeded++;
                    }
                    catch (Exception ex)
                    {
                        payload.Failed++;
                        payload.Errors.Add(new BatchSetParameterError
                        {
                            Index = i,
                            ElementId = op.ElementId,
                            Param = op.Param,
                            Message = ex.Message,
                        });
                        if (Setting.Atomic)
                        {
                            tx.RollBack();
                            payload.RolledBack = true;
                            payload.Succeeded = 0;
                            Result = new AIResult<BatchSetParametersResult>
                            {
                                Success = false,
                                Message = $"batch_set_parameters: rolled back at op #{i} ({op.Param} on {op.ElementId}): {ex.Message}",
                                Response = payload,
                            };
                            return;
                        }
                    }
                }

                tx.Commit();
                Result = new AIResult<BatchSetParametersResult>
                {
                    Success = true,
                    Message = $"batch_set_parameters: {payload.Succeeded}/{payload.Total} applied" +
                              (payload.Failed > 0 ? $" ({payload.Failed} failed, partial commit)" : ""),
                    Response = payload,
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<BatchSetParametersResult>
                {
                    Success = false,
                    Message = $"batch_set_parameters failed: {ex.Message}",
                };
            }
            finally
            {
                _resetEvent.Set();
            }
        }

        public bool WaitForCompletion(int timeoutMilliseconds = 30000)
        {
            _resetEvent.Reset();
            return _resetEvent.WaitOne(timeoutMilliseconds);
        }

        public string GetName() => "Batch Set Parameters";

        // ---- helpers ----

        private static void ApplyOne(Document doc, BatchSetParameterOperation op)
        {
            if (string.IsNullOrEmpty(op.Param))
                throw new ArgumentException("param is required");

            var el = doc.GetElement(new ElementId(op.ElementId));
            if (el == null)
                throw new ArgumentException($"element {op.ElementId} not found");

            var p = ResolveParameter(el, op.Param);
            if (p == null)
                throw new ArgumentException($"param '{op.Param}' not found on element {op.ElementId}");
            if (p.IsReadOnly)
                throw new ArgumentException($"param '{op.Param}' is read-only on element {op.ElementId}");

            SetParameterValue(p, op.Value);
        }

        private static Parameter ResolveParameter(Element el, string paramName)
        {
            // 1. Try BuiltInParameter enum name (e.g. "WALL_USER_HEIGHT_PARAM").
            if (Enum.TryParse<BuiltInParameter>(paramName, ignoreCase: false, out var bip))
            {
                var p = el.get_Parameter(bip);
                if (p != null) return p;
            }

            // 2. Fall back to LookupParameter (locale-friendly, e.g. "Hauteur").
            //    Revit LookupParameter is case-sensitive by default ; we try
            //    exact first, then a case-insensitive walk over the element's
            //    parameter set if no exact match.
            var direct = el.LookupParameter(paramName);
            if (direct != null) return direct;

            foreach (Parameter p in el.Parameters)
            {
                var defName = p?.Definition?.Name;
                if (defName != null && string.Equals(
                        defName, paramName, StringComparison.OrdinalIgnoreCase))
                    return p;
            }

            return null;
        }

        private static void SetParameterValue(Parameter p, object rawValue)
        {
            if (rawValue == null)
                throw new ArgumentException("value cannot be null");

            switch (p.StorageType)
            {
                case StorageType.Double:
                {
                    double v = Convert.ToDouble(rawValue, System.Globalization.CultureInfo.InvariantCulture);
                    // Length-typed parameters are stored internally in feet ;
                    // the rest of the v2-kg surface uses metres, so accept
                    // metres on the wire and convert. For non-length doubles
                    // (angles, currencies, dimensionless), pass through.
                    try
                    {
                        var unitId = p.GetUnitTypeId();
                        if (unitId != null && UnitUtils.IsMeasurableSpec(p.Definition.GetDataType()))
                        {
                            // Heuristic: treat the input as the parameter's
                            // displayed unit canonical form. For length on the
                            // metric Revit profile that's metres ; for other
                            // measurables (angles in radians, etc.) the input
                            // is taken as-is (the caller MUST know).
                            if (IsLengthSpec(p.Definition.GetDataType()))
                            {
                                v = UnitUtils.ConvertToInternalUnits(v, UnitTypeId.Meters);
                            }
                        }
                    }
                    catch
                    {
                        // GetUnitTypeId/GetDataType not available for this
                        // parameter — store the raw double.
                    }
                    p.Set(v);
                    break;
                }

                case StorageType.Integer:
                    p.Set(Convert.ToInt32(rawValue, System.Globalization.CultureInfo.InvariantCulture));
                    break;

                case StorageType.String:
                    p.Set(Convert.ToString(rawValue, System.Globalization.CultureInfo.InvariantCulture));
                    break;

                case StorageType.ElementId:
                {
                    long idValue = Convert.ToInt64(rawValue, System.Globalization.CultureInfo.InvariantCulture);
                    p.Set(new ElementId(idValue));
                    break;
                }

                default:
                    throw new ArgumentException($"unsupported storage type {p.StorageType}");
            }
        }

        private static bool IsLengthSpec(ForgeTypeId dataType)
        {
            // SpecTypeId.Length covers WALL_USER_HEIGHT_PARAM,
            // INSTANCE_SILL_HEIGHT_PARAM, INSTANCE_HEAD_HEIGHT_PARAM, etc.
            try { return SpecTypeId.Length.Equals(dataType); }
            catch { return false; }
        }
    }
}
