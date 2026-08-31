using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Models.MEP;
using RevitMCPCommandSet.Services.MEP;

namespace RevitMCPCommandSet.Commands.MEP
{
  public class CreateConduitCommand : ExternalEventCommandBase
  {
    private CreateConduitEventHandler _handler => (CreateConduitEventHandler)Handler;
    public override string CommandName => "create_conduit";
    public CreateConduitCommand(UIApplication uiApp)
        : base(new CreateConduitEventHandler(), uiApp)
    {
    }
    public override object Execute(JObject parameters, string requestId)
    {
      try
      {
        List<ConduitCreationInfo> data = new List<ConduitCreationInfo>();
        data = parameters["data"].ToObject<List<ConduitCreationInfo>>();
        if (data == null)
          throw new ArgumentNullException(nameof(data), "AI input data is null");
        _handler.SetParameters(data);
        if (RaiseAndWaitForCompletion(15000))
          return _handler.Result;
        else
          throw new TimeoutException("Create conduit operation timed out");
      }
      catch (Exception ex)
      {
        throw new Exception($"Failed to create conduit: {ex.Message}");
      }
    }
  }
}
