using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Models.Architecture;
using RevitMCPCommandSet.Services.Architecture;

namespace RevitMCPCommandSet.Commands.Architecture
{
    public class CreateReferencePlaneCommand : ExternalEventCommandBase
    {
        private CreateReferencePlaneEventHandler _handler => (CreateReferencePlaneEventHandler)Handler;

        public override string CommandName => "create_reference_plane";

        public CreateReferencePlaneCommand(UIApplication uiApp)
            : base(new CreateReferencePlaneEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                List<ReferencePlaneCreationInfo> data = parameters["data"].ToObject<List<ReferencePlaneCreationInfo>>();
                if (data == null)
                    throw new ArgumentNullException(nameof(data), "No reference plane data provided");

                _handler.SetParameters(data);

                if (RaiseAndWaitForCompletion(15000))
                {
                    return _handler.Result;
                }
                else
                {
                    throw new TimeoutException("Create reference plane operation timed out");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create reference plane: {ex.Message}");
            }
        }
    }
}
