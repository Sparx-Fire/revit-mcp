using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Models.MEP;
using RevitMCPCommandSet.Services.MEP;

namespace RevitMCPCommandSet.Commands.MEP
{
  public class CreateSweptShapeCommand : ExternalEventCommandBase
  {
    private CreateSweptShapeEventHandler _handler => (CreateSweptShapeEventHandler)Handler;
    public override string CommandName => "create_swept_shape";
    public CreateSweptShapeCommand(UIApplication uiApp)
        : base(new CreateSweptShapeEventHandler(), uiApp)
    {
    }
    public override object Execute(JObject parameters, string requestId)
    {
      try
      {
        List<SweptShapeCreationInfo> data = new List<SweptShapeCreationInfo>();
        data = parameters["data"].ToObject<List<SweptShapeCreationInfo>>();
        if (data == null)
          throw new ArgumentNullException(nameof(data), "AI input data is null");
        _handler.SetParameters(data);
        if (RaiseAndWaitForCompletion(30000))
          return _handler.Result;
        else
          throw new TimeoutException("Create swept shape operation timed out");
      }
      catch (Exception ex)
      {
        throw new Exception($"Failed to create swept shape: {ex.Message}");
      }
    }
  }
}
