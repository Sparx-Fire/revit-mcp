using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Models.MEP;
using RevitMCPCommandSet.Services.MEP;

namespace RevitMCPCommandSet.Commands.MEP
{
  public class CreateEquipmentCommand : ExternalEventCommandBase
  {
    private CreateEquipmentEventHandler _handler => (CreateEquipmentEventHandler)Handler;
    public override string CommandName => "create_equipment";
    public CreateEquipmentCommand(UIApplication uiApp)
        : base(new CreateEquipmentEventHandler(), uiApp)
    {
    }
    public override object Execute(JObject parameters, string requestId)
    {
      try
      {
        List<EquipmentCreationInfo> data = new List<EquipmentCreationInfo>();
        data = parameters["data"].ToObject<List<EquipmentCreationInfo>>();
        if (data == null)
          throw new ArgumentNullException(nameof(data), "AI input data is null");
        _handler.SetParameters(data);
        if (RaiseAndWaitForCompletion(15000))
          return _handler.Result;
        else
          throw new TimeoutException("Create equipment operation timed out");
      }
      catch (Exception ex)
      {
        throw new Exception($"Failed to create equipment: {ex.Message}");
      }
    }
  }
}
