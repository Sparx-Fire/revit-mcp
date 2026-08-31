using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Models.MEP;
using RevitMCPCommandSet.Services.MEP;

namespace RevitMCPCommandSet.Commands.MEP
{
  public class PlaceFamilyInstanceCommand : ExternalEventCommandBase
  {
    private PlaceFamilyInstanceEventHandler _handler => (PlaceFamilyInstanceEventHandler)Handler;
    public override string CommandName => "place_family_instance";
    public PlaceFamilyInstanceCommand(UIApplication uiApp)
        : base(new PlaceFamilyInstanceEventHandler(), uiApp)
    {
    }
    public override object Execute(JObject parameters, string requestId)
    {
      try
      {
        List<FamilyInstancePlacementInfo> data = new List<FamilyInstancePlacementInfo>();
        data = parameters["data"].ToObject<List<FamilyInstancePlacementInfo>>();
        if (data == null)
          throw new ArgumentNullException(nameof(data), "AI input data is null");
        _handler.SetParameters(data);
        if (RaiseAndWaitForCompletion(15000))
          return _handler.Result;
        else
          throw new TimeoutException("Place family instance operation timed out");
      }
      catch (Exception ex)
      {
        throw new Exception($"Failed to place family instance: {ex.Message}");
      }
    }
  }
}
