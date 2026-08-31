using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Models.MEP;
using RevitMCPCommandSet.Services.MEP;

namespace RevitMCPCommandSet.Commands.MEP
{
  public class CreateDirectShapeCommand : ExternalEventCommandBase
  {
    private CreateDirectShapeEventHandler _handler => (CreateDirectShapeEventHandler)Handler;
    public override string CommandName => "create_direct_shape";
    public CreateDirectShapeCommand(UIApplication uiApp)
        : base(new CreateDirectShapeEventHandler(), uiApp)
    {
    }
    public override object Execute(JObject parameters, string requestId)
    {
      try
      {
        List<DirectShapeCreationInfo> data = new List<DirectShapeCreationInfo>();
        data = parameters["data"].ToObject<List<DirectShapeCreationInfo>>();
        if (data == null)
          throw new ArgumentNullException(nameof(data), "AI input data is null");
        _handler.SetParameters(data);
        if (RaiseAndWaitForCompletion(30000))
          return _handler.Result;
        else
          throw new TimeoutException("Create direct shape operation timed out");
      }
      catch (Exception ex)
      {
        throw new Exception($"Failed to create direct shape: {ex.Message}");
      }
    }
  }
}
