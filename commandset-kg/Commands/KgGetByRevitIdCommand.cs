using System;
using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPKgCommandSet.Services;
using RevitMCPSDK.API.Base;

namespace RevitMCPKgCommandSet.Commands
{
    public class KgGetByRevitIdCommand : ExternalEventCommandBase
    {
        private KgGetByRevitIdEventHandler _handler => (KgGetByRevitIdEventHandler)Handler;

        public override string CommandName => "kg_get_by_revit_id";

        public KgGetByRevitIdCommand(UIApplication uiApp)
            : base(new KgGetByRevitIdEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                var tok = parameters?["revit_id"];
                if (tok == null || tok.Type == JTokenType.Null)
                    throw new ArgumentException("revit_id required");
                long revitId = tok.Value<long>();
                _handler.SetParameters(revitId);

                if (RaiseAndWaitForCompletion(10000))
                    return _handler.Result;

                throw new TimeoutException("kg_get_by_revit_id timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"kg_get_by_revit_id failed: {ex.Message}");
            }
        }
    }
}
