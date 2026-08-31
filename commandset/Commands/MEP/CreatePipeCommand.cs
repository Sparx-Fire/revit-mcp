using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Models.MEP;
using RevitMCPCommandSet.Services.MEP;

namespace RevitMCPCommandSet.Commands.MEP
{
  public class CreatePipeCommand : ExternalEventCommandBase
  {
    private CreatePipeEventHandler _handler => (CreatePipeEventHandler)Handler;
    public override string CommandName => "create_pipe";
    public CreatePipeCommand(UIApplication uiApp)
        : base(new CreatePipeEventHandler(), uiApp)
    {
    }
    public override object Execute(JObject parameters, string requestId)
    {
      try
      {
        List<PipeCreationInfo> data = new List<PipeCreationInfo>();
        data = parameters["data"].ToObject<List<PipeCreationInfo>>();
        if (data == null)
          throw new ArgumentNullException(nameof(data), "AI input data is null");
        _handler.SetParameters(data);
        if (RaiseAndWaitForCompletion(15000))
          return _handler.Result;
        else
          throw new TimeoutException("Create pipe operation timed out");
      }
      catch (Exception ex)
      {
        throw new Exception($"Failed to create pipe: {ex.Message}");
      }
    }
  }
}
