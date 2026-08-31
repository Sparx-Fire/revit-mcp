using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Models.Architecture;
using RevitMCPCommandSet.Services.Architecture;

namespace RevitMCPCommandSet.Commands.Architecture
{
    public class CreateOpeningCommand : ExternalEventCommandBase
    {
        private CreateOpeningEventHandler _handler => (CreateOpeningEventHandler)Handler;

        public override string CommandName => "create_opening";

        public CreateOpeningCommand(UIApplication uiApp)
            : base(new CreateOpeningEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                List<OpeningCreationInfo> data = parameters["data"].ToObject<List<OpeningCreationInfo>>();
                if (data == null)
                    throw new ArgumentNullException(nameof(data), "No opening data provided");

                _handler.SetParameters(data);

                if (RaiseAndWaitForCompletion(15000))
                {
                    return _handler.Result;
                }
                else
                {
                    throw new TimeoutException("Create opening operation timed out");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create opening: {ex.Message}");
            }
        }
    }
}
