using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Services.MEP;

namespace RevitMCPCommandSet.Commands.MEP
{
    public class CreateMEPCurveCommand : ExternalEventCommandBase
    {
        private CreateMEPCurveEventHandler _handler => (CreateMEPCurveEventHandler)Handler;

        public override string CommandName => "create_mep_curve";

        public CreateMEPCurveCommand(UIApplication uiApp)
            : base(new CreateMEPCurveEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                string mepType = parameters["mepType"]?.Value<string>() ?? "duct";
                JObject startObj = parameters["start"] as JObject;
                JObject endObj = parameters["end"] as JObject;

                double startX = startObj?["x"]?.Value<double>() ?? 0;
                double startY = startObj?["y"]?.Value<double>() ?? 0;
                double startZ = startObj?["z"]?.Value<double>() ?? 0;
                double endX = endObj?["x"]?.Value<double>() ?? 10;
                double endY = endObj?["y"]?.Value<double>() ?? 10;
                double endZ = endObj?["z"]?.Value<double>() ?? 0;

                double level = parameters["level"]?.Value<double>() ?? 0;
                double diameter = parameters["diameter"]?.Value<double>() ?? 200;
                string systemType = parameters["systemType"]?.Value<string>();

                _handler.SetParameters(mepType, startX, startY, startZ, endX, endY, endZ, level, diameter, systemType);

                if (RaiseAndWaitForCompletion(10000))
                    return _handler.Result;
                else
                    throw new TimeoutException("Create MEP curve operation timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create MEP curve: {ex.Message}");
            }
        }
    }
}
