using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Services.Views;

namespace RevitMCPCommandSet.Commands.Views
{
    public class SetViewPropertiesCommand : ExternalEventCommandBase
    {
        private SetViewPropertiesEventHandler _handler => (SetViewPropertiesEventHandler)Handler;

        public override string CommandName => "set_view_properties";

        public SetViewPropertiesCommand(UIApplication uiApp)
            : base(new SetViewPropertiesEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                int viewId = parameters["viewId"]?.Value<int>() ?? 0;
                JObject properties = parameters["properties"] as JObject;

                _handler.SetParameters(viewId, properties);

                if (RaiseAndWaitForCompletion(10000))
                    return _handler.Result;
                else
                    throw new TimeoutException("Set view properties operation timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to set view properties: {ex.Message}");
            }
        }
    }
}
