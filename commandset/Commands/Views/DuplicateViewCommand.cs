using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Services.Views;

namespace RevitMCPCommandSet.Commands.Views
{
    public class DuplicateViewCommand : ExternalEventCommandBase
    {
        private DuplicateViewEventHandler _handler => (DuplicateViewEventHandler)Handler;

        public override string CommandName => "duplicate_view";

        public DuplicateViewCommand(UIApplication uiApp)
            : base(new DuplicateViewEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                int viewId = parameters["viewId"]?.Value<int>() ?? 0;
                string mode = parameters["mode"]?.Value<string>() ?? "duplicate";
                string newName = parameters["newName"]?.Value<string>();

                _handler.SetParameters(viewId, mode, newName);

                if (RaiseAndWaitForCompletion(10000))
                    return _handler.Result;
                else
                    throw new TimeoutException("Duplicate view operation timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to duplicate view: {ex.Message}");
            }
        }
    }
}
