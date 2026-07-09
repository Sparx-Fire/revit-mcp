using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Models.Views;
using RevitMCPCommandSet.Services.Views;
using RevitMCPSDK.API.Base;

namespace RevitMCPCommandSet.Commands.Views;

public class CreateSheetCommand : ExternalEventCommandBase
{
    private CreateSheetEventHandler _handler => (CreateSheetEventHandler)Handler;

    public override string CommandName => "create_sheet";

    public CreateSheetCommand(UIApplication uiApp)
        : base(new CreateSheetEventHandler(), uiApp)
    {
    }

    public override object Execute(JObject parameters, string requestId)
    {
        try
        {
            var sheet = parameters?["sheet"]?.ToObject<SheetCreationInfo>()
                ?? parameters?.ToObject<SheetCreationInfo>();

            if (sheet == null)
                throw new ArgumentException("Sheet creation info is required.");

            _handler.SetParameters(sheet);

            if (RaiseAndWaitForCompletion(60000))
                return _handler.ResultInfo;

            throw new TimeoutException("Create sheet operation timed out after 60 seconds");
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to create sheet: {ex.Message}", ex);
        }
    }
}
