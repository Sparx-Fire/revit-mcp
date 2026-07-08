using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Models.Architecture;
using RevitMCPCommandSet.Services;
using RevitMCPSDK.API.Base;

namespace RevitMCPCommandSet.Commands;

public class ConfigureGridDisplayCommand : ExternalEventCommandBase
{
    private ConfigureGridDisplayEventHandler _handler => (ConfigureGridDisplayEventHandler)Handler;

    public ConfigureGridDisplayCommand(UIApplication uiApp)
        : base(new ConfigureGridDisplayEventHandler(), uiApp)
    {
    }

    public override string CommandName => "configure_grid_display";

    public override object Execute(JObject parameters, string requestId)
    {
        try
        {
            var options = parameters?.ToObject<GridDisplayConfigurationInfo>()
                ?? new GridDisplayConfigurationInfo();

            _handler.SetParameters(options);

            if (RaiseAndWaitForCompletion(30000))
                return _handler.Result;

            throw new TimeoutException("Configure grid display operation timed out after 30 seconds.");
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to configure grid display: {ex.Message}", ex);
        }
    }
}
