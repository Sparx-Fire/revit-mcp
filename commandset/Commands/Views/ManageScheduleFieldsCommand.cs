using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Services.Views;

namespace RevitMCPCommandSet.Commands.Views
{
    public class ManageScheduleFieldsCommand : ExternalEventCommandBase
    {
        private ManageScheduleFieldsEventHandler _handler => (ManageScheduleFieldsEventHandler)Handler;

        public override string CommandName => "manage_schedule_fields";

        public ManageScheduleFieldsCommand(UIApplication uiApp)
            : base(new ManageScheduleFieldsEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                int scheduleId = parameters["scheduleId"]?.Value<int>() ?? 0;
                string action = parameters["action"]?.Value<string>() ?? "add";
                string fieldName = parameters["fieldName"]?.Value<string>();
                int? position = parameters["position"]?.Value<int>();

                _handler.SetParameters(scheduleId, action, fieldName, position);

                if (RaiseAndWaitForCompletion(10000))
                    return _handler.Result;
                else
                    throw new TimeoutException("Manage schedule fields operation timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to manage schedule fields: {ex.Message}");
            }
        }
    }
}
