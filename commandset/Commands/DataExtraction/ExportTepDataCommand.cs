using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Services.DataExtraction;
using RevitMCPSDK.API.Base;

namespace RevitMCPCommandSet.Commands.DataExtraction
{
    public class ExportTepDataCommand : ExternalEventCommandBase
    {
        private ExportTepDataEventHandler _handler => (ExportTepDataEventHandler)Handler;

        public override string CommandName => "export_tep_data";

        public ExportTepDataCommand(UIApplication uiApp)
            : base(new ExportTepDataEventHandler(), uiApp)
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

                throw new TimeoutException("Export TEP data operation timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to export TEP data: {ex.Message}");
            }
        }
    }
}
