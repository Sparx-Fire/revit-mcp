using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Models.Architecture;
using RevitMCPCommandSet.Services.Architecture;

namespace RevitMCPCommandSet.Commands.Architecture
{
    public class CreateWallCommand : ExternalEventCommandBase
    {
        private CreateWallEventHandler _handler => (CreateWallEventHandler)Handler;

        public override string CommandName => "create_wall";

        public CreateWallCommand(UIApplication uiApp)
            : base(new CreateWallEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                List<WallCreationInfo> data = parameters["data"].ToObject<List<WallCreationInfo>>();
                if (data == null)
                    throw new ArgumentNullException(nameof(data), "No wall data provided");

                _handler.SetParameters(data);

                if (RaiseAndWaitForCompletion(15000))
                {
                    return _handler.Result;
                }
                else
                {
                    throw new TimeoutException("Create wall operation timed out");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create wall: {ex.Message}");
            }
        }
    }
}
