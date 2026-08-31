using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Models.Architecture;
using RevitMCPCommandSet.Services.Architecture;

namespace RevitMCPCommandSet.Commands.Architecture
{
    public class CreateCeilingCommand : ExternalEventCommandBase
    {
        private CreateCeilingEventHandler _handler => (CreateCeilingEventHandler)Handler;

        public override string CommandName => "create_ceiling";

        public CreateCeilingCommand(UIApplication uiApp)
            : base(new CreateCeilingEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                List<CeilingCreationInfo> data = parameters["data"].ToObject<List<CeilingCreationInfo>>();
                if (data == null)
                    throw new ArgumentNullException(nameof(data), "No ceiling data provided");

                _handler.SetParameters(data);

                if (RaiseAndWaitForCompletion(15000))
                {
                    return _handler.Result;
                }
                else
                {
                    throw new TimeoutException("Create ceiling operation timed out");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create ceiling: {ex.Message}");
            }
        }
    }
}
