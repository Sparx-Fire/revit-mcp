using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Models.MEP;
using RevitMCPCommandSet.Services.MEP;

namespace RevitMCPCommandSet.Commands.MEP
{
  public class CreateSpaceCommand : ExternalEventCommandBase
  {
    private CreateSpaceEventHandler _handler => (CreateSpaceEventHandler)Handler;
    public override string CommandName => "create_space";
    public CreateSpaceCommand(UIApplication uiApp)
        : base(new CreateSpaceEventHandler(), uiApp)
    {
    }
    public override object Execute(JObject parameters, string requestId)
    {
      try
      {
        List<SpaceCreationInfo> data = new List<SpaceCreationInfo>();
        data = parameters["data"].ToObject<List<SpaceCreationInfo>>();
        if (data == null)
          throw new ArgumentNullException(nameof(data), "AI input data is null");
        _handler.SetParameters(data);
        if (RaiseAndWaitForCompletion(15000))
          return _handler.Result;
        else
          throw new TimeoutException("Create space operation timed out");
      }
      catch (Exception ex)
      {
        throw new Exception($"Failed to create space: {ex.Message}");
      }
    }
  }
}
