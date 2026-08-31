using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Services.Views;

namespace RevitMCPCommandSet.Commands.Views
{
    public class CreateDraftingViewCommand : ExternalEventCommandBase
    {
        private CreateDraftingViewEventHandler _handler => (CreateDraftingViewEventHandler)Handler;

        public override string CommandName => "create_drafting_view";

        public CreateDraftingViewCommand(UIApplication uiApp)
            : base(new CreateDraftingViewEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                string name = parameters["name"]?.Value<string>();
                int scale = parameters["scale"]?.Value<int>() ?? 100;
                string detailLevel = parameters["detailLevel"]?.Value<string>() ?? "Coarse";

                _handler.SetParameters(name, scale, detailLevel);

                if (RaiseAndWaitForCompletion(10000))
                    return _handler.Result;
                else
                    throw new TimeoutException("Create drafting view operation timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create drafting view: {ex.Message}");
            }
        }
    }
}
