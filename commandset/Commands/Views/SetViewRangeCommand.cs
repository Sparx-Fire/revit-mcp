using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Services.Views;

namespace RevitMCPCommandSet.Commands.Views
{
    public class SetViewRangeCommand : ExternalEventCommandBase
    {
        private SetViewRangeEventHandler _handler => (SetViewRangeEventHandler)Handler;

        public override string CommandName => "set_view_range";

        public SetViewRangeCommand(UIApplication uiApp)
            : base(new SetViewRangeEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                int viewId = parameters["viewId"]?.Value<int>() ?? 0;
                double topOffset = parameters["topOffset"]?.Value<double>() ?? 0;
                double cutOffset = parameters["cutOffset"]?.Value<double>() ?? 0;
                double bottomOffset = parameters["bottomOffset"]?.Value<double>() ?? 0;
                double viewDepthOffset = parameters["viewDepthOffset"]?.Value<double>() ?? 0;
                int? topLevelId = parameters["topLevelId"]?.Value<int>();

                _handler.SetParameters(viewId, topOffset, cutOffset, bottomOffset, viewDepthOffset, topLevelId);

                if (RaiseAndWaitForCompletion(10000))
                    return _handler.Result;
                else
                    throw new TimeoutException("Set view range operation timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to set view range: {ex.Message}");
            }
        }
    }
}
