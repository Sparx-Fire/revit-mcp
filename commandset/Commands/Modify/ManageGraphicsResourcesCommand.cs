using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Services.Modify;

namespace RevitMCPCommandSet.Commands.Modify
{
    public class ManageGraphicsResourcesCommand : ExternalEventCommandBase
    {
        private ManageGraphicsResourcesEventHandler _handler => (ManageGraphicsResourcesEventHandler)Handler;

        public override string CommandName => "manage_graphics_resources";

        public ManageGraphicsResourcesCommand(UIApplication uiApp)
            : base(new ManageGraphicsResourcesEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                string action = parameters["action"]?.Value<string>() ?? "line_style";
                string name = parameters["name"]?.Value<string>();
                JObject properties = parameters["properties"] as JObject;

                _handler.SetParameters(action, name, properties);

                if (RaiseAndWaitForCompletion(10000))
                    return _handler.Result;
                else
                    throw new TimeoutException("Manage graphics resources operation timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to manage graphics resources: {ex.Message}");
            }
        }
    }
}
