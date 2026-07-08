using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Models.Views;
using RevitMCPCommandSet.Services.Views;
using RevitMCPSDK.API.Base;

namespace RevitMCPCommandSet.Commands.Views;

public class PlaceViewOnSheetCommand : ExternalEventCommandBase
{
    private PlaceViewOnSheetEventHandler _handler => (PlaceViewOnSheetEventHandler)Handler;

    public override string CommandName => "place_view_on_sheet";

    public PlaceViewOnSheetCommand(UIApplication uiApp)
        : base(new PlaceViewOnSheetEventHandler(), uiApp)
    {
    }

    public override object Execute(JObject parameters, string requestId)
    {
        try
        {
            var placement = parameters?["placement"]?.ToObject<ViewportCreationInfo>()
                ?? parameters?.ToObject<ViewportCreationInfo>();

            if (placement == null)
                throw new ArgumentException("Viewport placement info is required.");

            _handler.SetParameters(placement);

            if (RaiseAndWaitForCompletion(60000))
                return _handler.ResultInfo;

            throw new TimeoutException("Place view on sheet operation timed out after 60 seconds");
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to place view on sheet: {ex.Message}", ex);
        }
    }
}
