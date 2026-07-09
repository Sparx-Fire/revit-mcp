using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Services.DataExtraction;
using RevitMCPSDK.API.Base;

namespace RevitMCPCommandSet.Commands.DataExtraction
{
    public class ExportRoomFinishDataCommand : ExternalEventCommandBase
    {
        private ExportRoomFinishDataEventHandler _handler => (ExportRoomFinishDataEventHandler)Handler;

        public override string CommandName => "export_room_finish_data";

        public ExportRoomFinishDataCommand(UIApplication uiApp)
            : base(new ExportRoomFinishDataEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                bool includeUnplacedRooms = parameters?["includeUnplacedRooms"]?.Value<bool>() ?? false;
                bool includeNotEnclosedRooms = parameters?["includeNotEnclosedRooms"]?.Value<bool>() ?? false;

                _handler.SetParameters(includeUnplacedRooms, includeNotEnclosedRooms);

                if (RaiseAndWaitForCompletion(30000))
                {
                    return _handler.ResultInfo;
                }

                throw new TimeoutException("Export room finish data operation timed out after 30 seconds");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to export room finish data: {ex.Message}");
            }
        }
    }
}
