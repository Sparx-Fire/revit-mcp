using System;
using System.Collections.Generic;
using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPKgCommandSet.Services;
using RevitMCPSDK.API.Base;

namespace RevitMCPKgCommandSet.Commands
{
    public class KgAnnotateCommand : ExternalEventCommandBase
    {
        private KgAnnotateEventHandler _handler => (KgAnnotateEventHandler)Handler;

        public override string CommandName => "kg_annotate";

        public KgAnnotateCommand(UIApplication uiApp)
            : base(new KgAnnotateEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                var srcTok = parameters?["src"];
                var dstTok = parameters?["dst"];
                var kindTok = parameters?["kind"];
                if (srcTok == null || dstTok == null || kindTok == null)
                    throw new ArgumentException("kg_annotate requires src, dst, kind");

                string src = srcTok.ToString();
                string dst = dstTok.ToString();
                string kind = kindTok.ToString();

                // payload absent OR explicit JSON null both mean "delete the
                // annotation if it exists" (DESIGN §2.2: payload=null sentinel).
                var payloadTok = parameters?["payload"];
                bool payloadIsNull = payloadTok == null || payloadTok.Type == JTokenType.Null;
                Dictionary<string, object> payload = null;
                if (!payloadIsNull && payloadTok is JObject jo)
                    payload = jo.ToObject<Dictionary<string, object>>();
                else if (!payloadIsNull)
                    throw new ArgumentException("kg_annotate payload must be a JSON object or null");

                _handler.SetParameters(src, dst, kind, payload, payloadIsNull);

                if (RaiseAndWaitForCompletion(10000))
                    return _handler.Result;

                throw new TimeoutException("kg_annotate timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"kg_annotate failed: {ex.Message}");
            }
        }
    }
}
