using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Models.Views;
using RevitMCPCommandSet.Services.Views;

namespace RevitMCPCommandSet.Commands.Views
{
    public class CreateScheduleCommand : ExternalEventCommandBase
    {
        private CreateScheduleEventHandler _handler => (CreateScheduleEventHandler)Handler;

        public override string CommandName => "create_schedule";

        public CreateScheduleCommand(UIApplication uiApp)
            : base(new CreateScheduleEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                List<ScheduleCreationInfo> data = new List<ScheduleCreationInfo>();
                data = parameters["data"].ToObject<List<ScheduleCreationInfo>>();
                if (data == null)
                    throw new ArgumentNullException(nameof(data), "AI传入数据为空");

                _handler.SetParameters(data);

                if (RaiseAndWaitForCompletion(15000))
                    return _handler.Result;
                else
                    throw new TimeoutException("Create schedule operation timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create schedule: {ex.Message}");
            }
        }
    }
}
