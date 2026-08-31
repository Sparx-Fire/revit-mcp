using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Models.Architecture;
using RevitMCPCommandSet.Services.Architecture;

namespace RevitMCPCommandSet.Commands.Architecture
{
    public class CreateFloorCommand : ExternalEventCommandBase
    {
        private CreateFloorEventHandler _handler => (CreateFloorEventHandler)Handler;

        public override string CommandName => "create_floor";

        public CreateFloorCommand(UIApplication uiApp)
            : base(new CreateFloorEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                List<FloorInfo> data = parameters["data"].ToObject<List<FloorInfo>>();
                if (data == null)
                    throw new ArgumentNullException(nameof(data), "No floor data provided");

                _handler.SetParameters(data);

                if (RaiseAndWaitForCompletion(15000))
                {
                    return _handler.Result;
                }
                else
                {
                    throw new TimeoutException("Create floor operation timed out");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create floor: {ex.Message}");
            }
        }
    }
}
