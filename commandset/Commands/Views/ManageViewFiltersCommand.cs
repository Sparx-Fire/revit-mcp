using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Services.Views;

namespace RevitMCPCommandSet.Commands.Views
{
    public class ManageViewFiltersCommand : ExternalEventCommandBase
    {
        private ManageViewFiltersEventHandler _handler => (ManageViewFiltersEventHandler)Handler;

        public override string CommandName => "manage_view_filters";

        public ManageViewFiltersCommand(UIApplication uiApp)
            : base(new ManageViewFiltersEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                int viewId = parameters["viewId"]?.Value<int>() ?? 0;
                string action = parameters["action"]?.Value<string>() ?? "add";
                string filterName = parameters["filterName"]?.Value<string>();
                JObject overrides = parameters["overrides"] as JObject;

                _handler.SetParameters(viewId, action, filterName, overrides);

                if (RaiseAndWaitForCompletion(10000))
                    return _handler.Result;
                else
                    throw new TimeoutException("Manage view filters operation timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to manage view filters: {ex.Message}");
            }
        }
    }
}
