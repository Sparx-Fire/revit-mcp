using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Services.Modify;

namespace RevitMCPCommandSet.Commands.Modify
{
    public class RenameElementCommand : ExternalEventCommandBase
    {
        private RenameElementEventHandler _handler => (RenameElementEventHandler)Handler;
        public override string CommandName => "rename_element";
        public RenameElementCommand(UIApplication uiApp)
            : base(new RenameElementEventHandler(), uiApp)
        {
        }
        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                int elementId = parameters["elementId"].Value<int>();
                string newName = parameters["newName"].Value<string>();
                _handler.SetParameters(elementId, newName);
                if (RaiseAndWaitForCompletion(10000))
                    return _handler.Result;
                throw new TimeoutException("Rename element timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to rename element: {ex.Message}");
            }
        }
    }
}
