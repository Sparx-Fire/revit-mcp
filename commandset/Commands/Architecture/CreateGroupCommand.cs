using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Models.Architecture;
using RevitMCPCommandSet.Services.Architecture;

namespace RevitMCPCommandSet.Commands.Architecture
{
    public class CreateGroupCommand : ExternalEventCommandBase
    {
        private CreateGroupEventHandler _handler => (CreateGroupEventHandler)Handler;

        public override string CommandName => "create_group";

        public CreateGroupCommand(UIApplication uiApp)
            : base(new CreateGroupEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                List<GroupCreationInfo> data = parameters["data"].ToObject<List<GroupCreationInfo>>();
                if (data == null)
                    throw new ArgumentNullException(nameof(data), "No group data provided");

                _handler.SetParameters(data);

                if (RaiseAndWaitForCompletion(30000))
                {
                    return _handler.Result;
                }
                else
                {
                    throw new TimeoutException("Create group operation timed out");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create group: {ex.Message}");
            }
        }
    }
}
