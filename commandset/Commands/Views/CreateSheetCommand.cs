using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Models.Views;
using RevitMCPCommandSet.Services.Views;

namespace RevitMCPCommandSet.Commands.Views
{
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
                List<SheetCreationInfo> data = new List<SheetCreationInfo>();
                data = parameters["data"].ToObject<List<SheetCreationInfo>>();
                if (data == null)
                    throw new ArgumentNullException(nameof(data), "AI传入数据为空");

                _handler.SetParameters(data);

                if (RaiseAndWaitForCompletion(15000))
                    return _handler.Result;
                else
                    throw new TimeoutException("Create sheet operation timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create sheet: {ex.Message}");
            }
        }
    }
}
