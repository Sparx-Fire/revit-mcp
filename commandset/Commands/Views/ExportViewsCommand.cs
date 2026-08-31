using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Models.Views;
using RevitMCPCommandSet.Services.Views;

namespace RevitMCPCommandSet.Commands.Views
{
  public class ExportViewsCommand : ExternalEventCommandBase
  {
    private ExportViewsEventHandler _handler => (ExportViewsEventHandler)Handler;
    public override string CommandName => "export_views";
    public ExportViewsCommand(UIApplication uiApp)
        : base(new ExportViewsEventHandler(), uiApp)
    {
    }
    public override object Execute(JObject parameters, string requestId)
    {
      try
      {
        List<ExportSettingsInfo> data = new List<ExportSettingsInfo>();
        data = parameters["data"].ToObject<List<ExportSettingsInfo>>();
        if (data == null)
          throw new ArgumentNullException(nameof(data), "AI input data is null");
        _handler.SetParameters(data);
        if (RaiseAndWaitForCompletion(60000))
          return _handler.Result;
        else
          throw new TimeoutException("Export views operation timed out");
      }
      catch (Exception ex)
      {
        throw new Exception($"Failed to export views: {ex.Message}");
      }
    }
  }
}
