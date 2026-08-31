using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Services.Views;

namespace RevitMCPCommandSet.Commands.Views
{
    public class SetCategoryOverridesCommand : ExternalEventCommandBase
    {
        private SetCategoryOverridesEventHandler _handler => (SetCategoryOverridesEventHandler)Handler;

        public override string CommandName => "set_category_overrides";

        public SetCategoryOverridesCommand(UIApplication uiApp)
            : base(new SetCategoryOverridesEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                int viewId = parameters["viewId"]?.Value<int>() ?? 0;
                int categoryId = parameters["categoryId"]?.Value<int>() ?? 0;
                JObject overrides = parameters["overrides"] as JObject;

                _handler.SetParameters(viewId, categoryId, overrides);

                if (RaiseAndWaitForCompletion(10000))
                    return _handler.Result;
                else
                    throw new TimeoutException("Set category overrides operation timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to set category overrides: {ex.Message}");
            }
        }
    }
}
