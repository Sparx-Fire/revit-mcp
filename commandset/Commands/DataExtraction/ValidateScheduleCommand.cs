using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Services.DataExtraction;
using RevitMCPSDK.API.Base;

namespace RevitMCPCommandSet.Commands.DataExtraction
{
    public class ValidateScheduleCommand : ExternalEventCommandBase
    {
        private ValidateScheduleEventHandler _handler => (ValidateScheduleEventHandler)Handler;

        public override string CommandName => "validate_schedule";

        public ValidateScheduleCommand(UIApplication uiApp)
            : base(new ValidateScheduleEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                string category = parameters?["category"]?.Value<string>();
                string scheduleName = parameters?["scheduleName"]?.Value<string>();
                string levelName = parameters?["levelName"]?.Value<string>();
                long? levelId = parameters?["levelId"]?.Value<long?>();

                _handler.SetParameters(category, scheduleName, levelName, levelId);

                if (RaiseAndWaitForCompletion(120000))
                {
                    return _handler.ResultInfo;
                }

                throw new TimeoutException("Schedule validation timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to validate schedule: {ex.Message}");
            }
        }
    }
}
