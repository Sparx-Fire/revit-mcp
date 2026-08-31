using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Services.Views;

namespace RevitMCPCommandSet.Commands.Views
{
    public class CreateCalloutCommand : ExternalEventCommandBase
    {
        private CreateCalloutEventHandler _handler => (CreateCalloutEventHandler)Handler;

        public override string CommandName => "create_callout";

        public CreateCalloutCommand(UIApplication uiApp)
            : base(new CreateCalloutEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                string name = parameters["name"]?.Value<string>();
                int hostViewId = parameters["hostViewId"]?.Value<int>() ?? 0;
                JObject bbox = parameters["boundingBox"] as JObject;

                double minX = bbox?["minX"]?.Value<double>() ?? 0;
                double minY = bbox?["minY"]?.Value<double>() ?? 0;
                double maxX = bbox?["maxX"]?.Value<double>() ?? 10;
                double maxY = bbox?["maxY"]?.Value<double>() ?? 10;

                _handler.SetParameters(name, hostViewId, minX, minY, maxX, maxY);

                if (RaiseAndWaitForCompletion(10000))
                    return _handler.Result;
                else
                    throw new TimeoutException("Create callout operation timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create callout: {ex.Message}");
            }
        }
    }
}
