using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Services.Modify;

namespace RevitMCPCommandSet.Commands.Modify
{
    public class SetParametersCommand : ExternalEventCommandBase
    {
        private SetParametersEventHandler _handler => (SetParametersEventHandler)Handler;
        public override string CommandName => "set_parameters";
        public SetParametersCommand(UIApplication uiApp)
            : base(new SetParametersEventHandler(), uiApp)
        {
        }
        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                int elementId = parameters["elementId"].Value<int>();
                var paramValues = parameters["parameters"] as JObject;
                if (paramValues == null)
                    throw new ArgumentException("parameters object is required");
                _handler.SetParameters(elementId, paramValues);
                if (RaiseAndWaitForCompletion(10000))
                    return _handler.Result;
                throw new TimeoutException("Set parameters timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to set parameters: {ex.Message}");
            }
        }
    }
}
