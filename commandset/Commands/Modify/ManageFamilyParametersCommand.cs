using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Services.Modify;

namespace RevitMCPCommandSet.Commands.Modify
{
    public class ManageFamilyParametersCommand : ExternalEventCommandBase
    {
        private ManageFamilyParametersEventHandler _handler => (ManageFamilyParametersEventHandler)Handler;
        public override string CommandName => "manage_family_parameters";
        public ManageFamilyParametersCommand(UIApplication uiApp)
            : base(new ManageFamilyParametersEventHandler(), uiApp)
        {
        }
        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                string action = parameters["action"].Value<string>();
                int familyId = parameters["familyId"].Value<int>();
                string name = parameters["name"]?.Value<string>();
                string newName = parameters["newName"]?.Value<string>();
                string formula = parameters["formula"]?.Value<string>();
                string paramType = parameters["type"]?.Value<string>();
                _handler.SetParameters(action, familyId, name, newName, formula, paramType);
                if (RaiseAndWaitForCompletion(10000))
                    return _handler.Result;
                throw new TimeoutException("Manage family parameters timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to manage family parameters: {ex.Message}");
            }
        }
    }
}
