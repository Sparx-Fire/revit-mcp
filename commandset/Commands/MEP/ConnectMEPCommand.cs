using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Models.MEP;
using RevitMCPCommandSet.Services.MEP;

namespace RevitMCPCommandSet.Commands.MEP
{
  public class ConnectMEPCommand : ExternalEventCommandBase
  {
    private ConnectMEPEventHandler _handler => (ConnectMEPEventHandler)Handler;
    public override string CommandName => "connect_mep";
    public ConnectMEPCommand(UIApplication uiApp)
        : base(new ConnectMEPEventHandler(), uiApp)
    {
    }
    public override object Execute(JObject parameters, string requestId)
    {
      try
      {
        List<MEPConnectInfo> data = new List<MEPConnectInfo>();
        data = parameters["data"].ToObject<List<MEPConnectInfo>>();
        if (data == null)
          throw new ArgumentNullException(nameof(data), "AI input data is null");
        _handler.SetParameters(data);
        if (RaiseAndWaitForCompletion(15000))
          return _handler.Result;
        else
          throw new TimeoutException("Connect MEP operation timed out");
      }
      catch (Exception ex)
      {
        throw new Exception($"Failed to connect MEP: {ex.Message}");
      }
    }
  }
}
