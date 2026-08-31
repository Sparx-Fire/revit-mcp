using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Models.Architecture;
using RevitMCPCommandSet.Services.Architecture;

namespace RevitMCPCommandSet.Commands.Architecture
{
    public class CreateRampCommand : ExternalEventCommandBase
    {
        private CreateRampEventHandler _handler => (CreateRampEventHandler)Handler;

        public override string CommandName => "create_ramp";

        public CreateRampCommand(UIApplication uiApp)
            : base(new CreateRampEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                List<RampCreationInfo> data = parameters["data"].ToObject<List<RampCreationInfo>>();
                if (data == null)
                    throw new ArgumentNullException(nameof(data), "No ramp data provided");

                _handler.SetParameters(data);

                if (RaiseAndWaitForCompletion(15000))
                {
                    return _handler.Result;
                }
                else
                {
                    throw new TimeoutException("Create ramp operation timed out");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create ramp: {ex.Message}");
            }
        }
    }
}
