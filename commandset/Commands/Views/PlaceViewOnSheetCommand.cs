using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Models.Views;
using RevitMCPCommandSet.Services.Views;

namespace RevitMCPCommandSet.Commands.Views
{
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
                List<ViewportCreationInfo> data = new List<ViewportCreationInfo>();
                data = parameters["data"].ToObject<List<ViewportCreationInfo>>();
                if (data == null)
                    throw new ArgumentNullException(nameof(data), "AI传入数据为空");

                _handler.SetParameters(data);

                if (RaiseAndWaitForCompletion(15000))
                    return _handler.Result;
                else
                    throw new TimeoutException("Place viewport operation timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to place view on sheet: {ex.Message}");
            }
        }
    }
}
