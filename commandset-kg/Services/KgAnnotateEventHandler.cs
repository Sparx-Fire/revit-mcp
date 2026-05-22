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
    public class KgAnnotateEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public string Src { get; private set; }
        public string Dst { get; private set; }
        public string Kind { get; private set; }
        public Dictionary<string, object> Payload { get; private set; }
        public bool PayloadIsNull { get; private set; }

        public AIResult<KgAnnotateResult> Result { get; private set; }

        public void SetParameters(string src, string dst, string kind,
                                  Dictionary<string, object> payload, bool payloadIsNull)
        {
            Src = src;
            Dst = dst;
            Kind = kind;
            Payload = payload;
            PayloadIsNull = payloadIsNull;
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
                    Result = new AIResult<KgAnnotateResult>
                    {
                        Success = false,
                        Message = "kg_annotate: no active KG v2 projection",
                    };
                    return;
                }

                var op = kg.Annotate(Src, Dst, Kind, PayloadIsNull ? null : Payload);
                KgV2DocumentWatcher.FlushCurrent();

                Result = new AIResult<KgAnnotateResult>
                {
                    Success = true,
                    Message = $"KG v2 annotate {Kind}: {op}",
                    Response = new KgAnnotateResult
                    {
                        Operation = op,
                        Src = Src,
                        Dst = Dst,
                        Kind = Kind,
                        Turn = kg.Turn,
                    },
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<KgAnnotateResult>
                {
                    Success = false,
                    Message = $"kg_annotate failed: {ex.Message}",
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

        public string GetName() => "KG Annotate";
    }
}
