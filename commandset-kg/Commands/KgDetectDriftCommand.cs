using System;
using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPKgCommandSet.Services;
using RevitMCPSDK.API.Base;

namespace RevitMCPKgCommandSet.Commands
{
    public class KgDetectDriftCommand : ExternalEventCommandBase
    {
        private KgDetectDriftEventHandler _handler => (KgDetectDriftEventHandler)Handler;

        public override string CommandName => "kg_detect_drift";

        public KgDetectDriftCommand(UIApplication uiApp)
            : base(new KgDetectDriftEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                string nodeTypeFilter = parameters?["node_type"]?.ToString();
                if (string.IsNullOrWhiteSpace(nodeTypeFilter)) nodeTypeFilter = null;

                _handler.SetParameters(nodeTypeFilter);

                if (RaiseAndWaitForCompletion(15000))
                    return _handler.Result;

                throw new TimeoutException("kg_detect_drift timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"kg_detect_drift failed: {ex.Message}");
            }
        }
    }
}
