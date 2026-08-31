using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Services.Views;

namespace RevitMCPCommandSet.Commands.Views
{
    public class CreateDetailCurveCommand : ExternalEventCommandBase
    {
        private CreateDetailCurveEventHandler _handler => (CreateDetailCurveEventHandler)Handler;

        public override string CommandName => "create_detail_curve";

        public CreateDetailCurveCommand(UIApplication uiApp)
            : base(new CreateDetailCurveEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                int viewId = parameters["viewId"]?.Value<int>() ?? 0;
                JArray linesArray = parameters["lines"] as JArray;

                List<JObject> lines = new List<JObject>();
                if (linesArray != null)
                {
                    foreach (var item in linesArray)
                    {
                        lines.Add(item as JObject);
                    }
                }

                _handler.SetParameters(viewId, lines);

                if (RaiseAndWaitForCompletion(10000))
                    return _handler.Result;
                else
                    throw new TimeoutException("Create detail curve operation timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create detail curve: {ex.Message}");
            }
        }
    }
}
