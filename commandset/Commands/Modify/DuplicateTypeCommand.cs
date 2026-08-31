using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Services.Modify;

namespace RevitMCPCommandSet.Commands.Modify
{
    public class DuplicateTypeCommand : ExternalEventCommandBase
    {
        private DuplicateTypeEventHandler _handler => (DuplicateTypeEventHandler)Handler;
        public override string CommandName => "duplicate_type";
        public DuplicateTypeCommand(UIApplication uiApp)
            : base(new DuplicateTypeEventHandler(), uiApp)
        {
        }
        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                int typeId = parameters["typeId"].Value<int>();
                string newName = parameters["newName"].Value<string>();
                _handler.SetParameters(typeId, newName);
                if (RaiseAndWaitForCompletion(10000))
                    return _handler.Result;
                throw new TimeoutException("Duplicate type timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to duplicate type: {ex.Message}");
            }
        }
    }
}
