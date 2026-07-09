using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Services;
using RevitMCPSDK.API.Base;

namespace RevitMCPCommandSet.Commands;

public class GetDocumentStylesCommand : ExternalEventCommandBase
{
    private GetDocumentStylesEventHandler _handler => (GetDocumentStylesEventHandler)Handler;

    public override string CommandName => "get_document_styles";

    public GetDocumentStylesCommand(UIApplication uiApp)
        : base(new GetDocumentStylesEventHandler(), uiApp)
    {
    }

    public override object Execute(JObject parameters, string requestId)
    {
        if (RaiseAndWaitForCompletion(30000))
            return _handler.ResultInfo;

        throw new TimeoutException("Get document styles operation timed out after 30 seconds.");
    }
}
