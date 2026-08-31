using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Models.Architecture;
using RevitMCPCommandSet.Services.Architecture;

namespace RevitMCPCommandSet.Commands.Architecture
{
    public class CreateModelCurveCommand : ExternalEventCommandBase
    {
        private CreateModelCurveEventHandler _handler => (CreateModelCurveEventHandler)Handler;

        public override string CommandName => "create_model_curve";

        public CreateModelCurveCommand(UIApplication uiApp)
            : base(new CreateModelCurveEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                var dataList = new List<ModelCurveCreationInfo>();
                var rawData = parameters["data"] as JArray;

                if (rawData == null)
                    throw new ArgumentNullException(nameof(rawData), "No model curve data provided");

                foreach (var item in rawData)
                {
                    if (item is JObject obj)
                    {
                        // Try standard deserialization first
                        var info = obj.ToObject<ModelCurveCreationInfo>();

                        // If points is empty but startPoint/endPoint are present, convert them
                        if ((info.Points == null || info.Points.Count == 0) &&
                            obj["startPoint"] != null && obj["endPoint"] != null)
                        {
                            info.Points = new List<JZPoint>
                            {
                                obj["startPoint"].ToObject<JZPoint>(),
                                obj["endPoint"].ToObject<JZPoint>()
                            };
                        }

                        // Map sketchPlaneLevel (double elevation) to sketchPlaneId (int element ID)
                        // Not a direct mapping; sketchPlaneLevel is kept as-is; the handler will auto-create plane
                        dataList.Add(info);
                    }
                }

                if (dataList.Count == 0)
                    throw new ArgumentException("No valid model curve data provided");

                _handler.SetParameters(dataList);

                if (RaiseAndWaitForCompletion(15000))
                {
                    return _handler.Result;
                }
                else
                {
                    throw new TimeoutException("Create model curve operation timed out");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create model curve: {ex.Message}");
            }
        }
    }
}
