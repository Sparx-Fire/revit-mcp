using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Services.Query;

namespace RevitMCPCommandSet.Commands.Query
{
    public class CheckInterferencesCommand : ExternalEventCommandBase
    {
        private CheckInterferencesEventHandler _handler => (CheckInterferencesEventHandler)Handler;
        public override string CommandName => "check_interferences";
        public CheckInterferencesCommand(UIApplication uiApp)
            : base(new CheckInterferencesEventHandler(), uiApp)
        {
        }
        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                var elementIds = parameters["elementIds"].ToObject<int[]>();
                _handler.SetParameters(elementIds);
                if (RaiseAndWaitForCompletion(30000))
                    return _handler.Result;
                throw new TimeoutException("Check interferences timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to check interferences: {ex.Message}");
            }
        }
    }
}
