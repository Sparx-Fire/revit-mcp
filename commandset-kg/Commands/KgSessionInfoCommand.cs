using System;
using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPKgCommandSet.Services;
using RevitMCPSDK.API.Base;

namespace RevitMCPKgCommandSet.Commands
{
    public class KgSessionInfoCommand : ExternalEventCommandBase
    {
        private KgSessionInfoEventHandler _handler => (KgSessionInfoEventHandler)Handler;

        public override string CommandName => "kg_session_info";

        public KgSessionInfoCommand(UIApplication uiApp)
            : base(new KgSessionInfoEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                if (RaiseAndWaitForCompletion(10000))
                    return _handler.Result;

                throw new TimeoutException("kg_session_info timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"kg_session_info failed: {ex.Message}");
            }
        }
    }
}
