using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Models.Views;
using RevitMCPCommandSet.Services.Views;

namespace RevitMCPCommandSet.Commands.Views
{
    public class CreateViewCommand : ExternalEventCommandBase
    {
        private CreateViewEventHandler _handler => (CreateViewEventHandler)Handler;

        public override string CommandName => "create_view";

        public CreateViewCommand(UIApplication uiApp)
            : base(new CreateViewEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                List<ViewCreationInfo> data = new List<ViewCreationInfo>();
                data = parameters["data"].ToObject<List<ViewCreationInfo>>();
                if (data == null)
                    throw new ArgumentNullException(nameof(data), "AI传入数据为空");

                _handler.SetParameters(data);

                if (RaiseAndWaitForCompletion(15000))
                    return _handler.Result;
                else
                    throw new TimeoutException("Create view operation timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create view: {ex.Message}");
            }
        }
    }
}
