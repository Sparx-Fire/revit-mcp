using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Models.Architecture;
using RevitMCPCommandSet.Services.Architecture;

namespace RevitMCPCommandSet.Commands.Architecture
{
    public class CreateColumnCommand : ExternalEventCommandBase
    {
        private CreateColumnEventHandler _handler => (CreateColumnEventHandler)Handler;

        public override string CommandName => "create_column";

        public CreateColumnCommand(UIApplication uiApp)
            : base(new CreateColumnEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                List<ColumnInfo> data = parameters["data"].ToObject<List<ColumnInfo>>();
                if (data == null)
                    throw new ArgumentNullException(nameof(data), "No column data provided");

                _handler.SetParameters(data);

                if (RaiseAndWaitForCompletion(15000))
                {
                    return _handler.Result;
                }
                else
                {
                    throw new TimeoutException("Create column operation timed out");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create column: {ex.Message}");
            }
        }
    }
}
