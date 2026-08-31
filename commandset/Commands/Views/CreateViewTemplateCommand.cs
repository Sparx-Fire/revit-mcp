using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Services.Views;

namespace RevitMCPCommandSet.Commands.Views
{
    public class CreateViewTemplateCommand : ExternalEventCommandBase
    {
        private CreateViewTemplateEventHandler _handler => (CreateViewTemplateEventHandler)Handler;

        public override string CommandName => "create_view_template";

        public CreateViewTemplateCommand(UIApplication uiApp)
            : base(new CreateViewTemplateEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                int sourceViewId = parameters["sourceViewId"]?.Value<int>() ?? 0;
                string name = parameters["name"]?.Value<string>();

                _handler.SetParameters(sourceViewId, name);

                if (RaiseAndWaitForCompletion(10000))
                    return _handler.Result;
                else
                    throw new TimeoutException("Create view template operation timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create view template: {ex.Message}");
            }
        }
    }
}
