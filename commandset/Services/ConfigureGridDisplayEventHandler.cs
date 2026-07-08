using Autodesk.Revit.UI;
using RevitMCPCommandSet.Models.Architecture;
using RevitMCPCommandSet.Models.Common;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services;

public class ConfigureGridDisplayEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
{
    private GridDisplayConfigurationInfo _options;
    private readonly ManualResetEvent _resetEvent = new(false);

    public AIResult<GridDisplayConfigurationResult> Result { get; private set; }

    public void SetParameters(GridDisplayConfigurationInfo options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _resetEvent.Reset();
    }

    public void Execute(UIApplication app)
    {
        try
        {
            var doc = app.ActiveUIDocument?.Document
                ?? throw new InvalidOperationException("No active Revit document.");

            var grids = GridDisplayHelper.GetGrids(doc, _options.GridIds);

            GridDisplayConfigurationResult configurationResult;
            using (var transaction = new Transaction(doc, "Configure Grid Display"))
            {
                transaction.Start();
                configurationResult = GridDisplayHelper.ConfigureGrids(doc, grids, _options);
                transaction.Commit();
            }

            Result = new AIResult<GridDisplayConfigurationResult>
            {
                Success = true,
                Message =
                    $"Configured display for {configurationResult.GridsProcessed} grid(s) across {configurationResult.ViewsProcessed} floor plan(s).",
                Response = configurationResult
            };
        }
        catch (Exception ex)
        {
            Result = new AIResult<GridDisplayConfigurationResult>
            {
                Success = false,
                Message = $"Failed to configure grid display: {ex.Message}",
                Response = new GridDisplayConfigurationResult()
            };
        }
        finally
        {
            _resetEvent.Set();
        }
    }

    public bool WaitForCompletion(int timeoutMilliseconds = 10000)
    {
        _resetEvent.Reset();
        return _resetEvent.WaitOne(timeoutMilliseconds);
    }

    public string GetName() => "Configure Grid Display";
}
