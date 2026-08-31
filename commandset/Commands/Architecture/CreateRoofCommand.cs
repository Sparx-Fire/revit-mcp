using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Models.Architecture;
using RevitMCPCommandSet.Services.Architecture;

namespace RevitMCPCommandSet.Commands.Architecture
{
    public class CreateRoofCommand : ExternalEventCommandBase
    {
        private CreateRoofEventHandler _handler => (CreateRoofEventHandler)Handler;

        public override string CommandName => "create_roof";

        public CreateRoofCommand(UIApplication uiApp)
            : base(new CreateRoofEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                List<RoofInfo> data = parameters["data"].ToObject<List<RoofInfo>>();
                if (data == null)
                    throw new ArgumentNullException(nameof(data), "No roof data provided");

                _handler.SetParameters(data);

                if (RaiseAndWaitForCompletion(15000))
                {
                    return _handler.Result;
                }
                else
                {
                    throw new TimeoutException("Create roof operation timed out");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create roof: {ex.Message}");
            }
        }
    }
}
