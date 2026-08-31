using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Models.MEP;
using RevitMCPCommandSet.Services.MEP;

namespace RevitMCPCommandSet.Commands.MEP
{
  public class CreateDuctCommand : ExternalEventCommandBase
  {
    private CreateDuctEventHandler _handler => (CreateDuctEventHandler)Handler;
    public override string CommandName => "create_duct";
    public CreateDuctCommand(UIApplication uiApp)
        : base(new CreateDuctEventHandler(), uiApp)
    {
    }
    public override object Execute(JObject parameters, string requestId)
    {
      try
      {
        List<DuctCreationInfo> data = new List<DuctCreationInfo>();
        data = parameters["data"].ToObject<List<DuctCreationInfo>>();
        if (data == null)
          throw new ArgumentNullException(nameof(data), "AI input data is null");
        _handler.SetParameters(data);
        if (RaiseAndWaitForCompletion(15000))
          return _handler.Result;
        else
          throw new TimeoutException("Create duct operation timed out");
      }
      catch (Exception ex)
      {
        throw new Exception($"Failed to create duct: {ex.Message}");
      }
    }
  }
}
