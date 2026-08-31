using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Models.Architecture;
using RevitMCPCommandSet.Services.Architecture;

namespace RevitMCPCommandSet.Commands.Architecture
{
    public class CreateRailingCommand : ExternalEventCommandBase
    {
        private CreateRailingEventHandler _handler => (CreateRailingEventHandler)Handler;

        public override string CommandName => "create_railing";

        public CreateRailingCommand(UIApplication uiApp)
            : base(new CreateRailingEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                List<RailingCreationInfo> data = parameters["data"].ToObject<List<RailingCreationInfo>>();
                if (data == null)
                    throw new ArgumentNullException(nameof(data), "No railing data provided");

                _handler.SetParameters(data);

                if (RaiseAndWaitForCompletion(15000))
                {
                    return _handler.Result;
                }
                else
                {
                    throw new TimeoutException("Create railing operation timed out");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create railing: {ex.Message}");
            }
        }
    }
}
