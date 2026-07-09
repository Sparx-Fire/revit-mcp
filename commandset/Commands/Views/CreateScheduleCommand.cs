using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Models.Views;
using RevitMCPCommandSet.Services.Views;
using RevitMCPSDK.API.Base;

namespace RevitMCPCommandSet.Commands.Views;

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
            var schedule = parameters?["schedule"]?.ToObject<ScheduleCreationInfo>()
                ?? parameters?.ToObject<ScheduleCreationInfo>();

            if (schedule == null)
                throw new ArgumentException("Schedule creation info is required.");

            _handler.SetParameters(schedule);

            if (RaiseAndWaitForCompletion(120000))
                return _handler.ResultInfo;

            throw new TimeoutException("Create schedule operation timed out after 120 seconds");
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to create schedule: {ex.Message}", ex);
        }
    }
}
