using System;
using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPKgCommandSet.Services;
using RevitMCPSDK.API.Base;

namespace RevitMCPKgCommandSet.Commands
{
    public class KgDiffSinceCommand : ExternalEventCommandBase
    {
        private KgDiffSinceEventHandler _handler => (KgDiffSinceEventHandler)Handler;

        public override string CommandName => "kg_diff_since";

        public KgDiffSinceCommand(UIApplication uiApp)
            : base(new KgDiffSinceEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                int sinceTurn = parameters?["since_turn"]?.Value<int>() ?? 0;
                _handler.SetParameters(sinceTurn);

                if (RaiseAndWaitForCompletion(10000))
                    return _handler.Result;

                throw new TimeoutException("kg_diff_since timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"kg_diff_since failed: {ex.Message}");
            }
        }
    }
}
