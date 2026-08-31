using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Services;

namespace RevitMCPCommandSet.Commands
{
    public class SaveDocumentCommand : ExternalEventCommandBase
    {
        private SaveDocumentEventHandler _handler => (SaveDocumentEventHandler)Handler;

        public override string CommandName => "save_document";

        public SaveDocumentCommand(UIApplication uiApp)
            : base(new SaveDocumentEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                _handler.SetParameters();

                if (RaiseAndWaitForCompletion(30000))
                    return _handler.Result;
                else
                    throw new TimeoutException("Save document operation timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to save document: {ex.Message}");
            }
        }
    }
}
