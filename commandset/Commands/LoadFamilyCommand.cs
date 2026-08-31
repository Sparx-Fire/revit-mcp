using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Services;

namespace RevitMCPCommandSet.Commands
{
    public class LoadFamilyCommand : ExternalEventCommandBase
    {
        private LoadFamilyEventHandler _handler => (LoadFamilyEventHandler)Handler;

        public override string CommandName => "load_family";

        public LoadFamilyCommand(UIApplication uiApp)
            : base(new LoadFamilyEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                string filePath = parameters["filePath"]?.Value<string>();
                string familyName = parameters["familyName"]?.Value<string>();

                _handler.SetParameters(filePath, familyName);

                if (RaiseAndWaitForCompletion(30000))
                    return _handler.Result;
                else
                    throw new TimeoutException("Load family operation timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to load family: {ex.Message}");
            }
        }
    }
}
