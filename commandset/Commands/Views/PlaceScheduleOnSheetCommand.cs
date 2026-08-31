using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Services.Views;

namespace RevitMCPCommandSet.Commands.Views
{
    public class PlaceScheduleOnSheetCommand : ExternalEventCommandBase
    {
        private PlaceScheduleOnSheetEventHandler _handler => (PlaceScheduleOnSheetEventHandler)Handler;

        public override string CommandName => "place_schedule_on_sheet";

        public PlaceScheduleOnSheetCommand(UIApplication uiApp)
            : base(new PlaceScheduleOnSheetEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                int scheduleId = parameters["scheduleId"]?.Value<int>() ?? 0;
                int sheetId = parameters["sheetId"]?.Value<int>() ?? 0;
                JObject location = parameters["location"] as JObject;

                double x = location?["x"]?.Value<double>() ?? 0;
                double y = location?["y"]?.Value<double>() ?? 0;

                _handler.SetParameters(scheduleId, sheetId, x, y);

                if (RaiseAndWaitForCompletion(10000))
                    return _handler.Result;
                else
                    throw new TimeoutException("Place schedule on sheet operation timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to place schedule on sheet: {ex.Message}");
            }
        }
    }
}
