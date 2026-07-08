using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Models.Views;
using RevitMCPCommandSet.Services.Views;
using RevitMCPSDK.API.Base;

namespace RevitMCPCommandSet.Commands.Views;

public class CreateFinishScheduleCommand : ExternalEventCommandBase
{
    private CreateFinishScheduleEventHandler _handler => (CreateFinishScheduleEventHandler)Handler;

    public override string CommandName => "create_finish_schedule";

    public CreateFinishScheduleCommand(UIApplication uiApp)
        : base(new CreateFinishScheduleEventHandler(), uiApp)
    {
    }

    public override object Execute(JObject parameters, string requestId)
    {
        try
        {
            var info = parameters?["schedule"]?.ToObject<FinishScheduleCreationInfo>()
                ?? parameters?.ToObject<FinishScheduleCreationInfo>();

            if (info == null)
                throw new ArgumentException("Finish schedule creation info is required.");

            _handler.SetParameters(info);

            if (RaiseAndWaitForCompletion(60000))
                return _handler.ResultInfo;

            throw new TimeoutException("Create finish schedule operation timed out after 60 seconds");
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to create finish schedule: {ex.Message}", ex);
        }
    }
}
