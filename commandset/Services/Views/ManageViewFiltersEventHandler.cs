using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Models.Common;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services.Views
{
    public class ManageViewFiltersEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private UIDocument uiDoc => uiApp.ActiveUIDocument;
        private Document doc => uiDoc.Document;

        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public int ViewId { get; private set; }
        public string Action { get; private set; }
        public string FilterName { get; private set; }
        public JObject Overrides { get; private set; }

        public AIResult<bool> Result { get; private set; }

        public void SetParameters(int viewId, string action, string filterName, JObject overrides)
        {
            ViewId = viewId;
            Action = action;
            FilterName = filterName;
            Overrides = overrides;
            _resetEvent.Reset();
        }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;

            try
            {
                using (Transaction trans = new Transaction(doc, "Manage View Filters"))
                {
                    trans.Start();

                    View view = doc.GetElement(new ElementId(ViewId)) as View;
                    if (view == null)
                    {
                        Result = new AIResult<bool> { Success = false, Message = $"View with ID {ViewId} not found" };
                        return;
                    }

                    ParameterFilterElement filter = new FilteredElementCollector(doc)
                        .OfClass(typeof(ParameterFilterElement))
                        .Cast<ParameterFilterElement>()
                        .FirstOrDefault(f => f.Name == FilterName);

                    if (filter == null)
                    {
                        Result = new AIResult<bool> { Success = false, Message = $"Filter '{FilterName}' not found" };
                        return;
                    }

                    ElementId filterId = filter.Id;

                    switch (Action.ToLowerInvariant())
                    {
                        case "add":
                            view.AddFilter(filterId);

                            if (Overrides != null)
                            {
                                OverrideGraphicSettings overrideSettings = new OverrideGraphicSettings();

                                if (Overrides["visible"] != null)
                                {
                                    bool visible = Overrides["visible"].Value<bool>();
                                    view.SetFilterVisibility(filterId, visible);
                                }

                                if (Overrides["color"] != null)
                                {
                                    JObject colorObj = Overrides["color"] as JObject;
                                    if (colorObj != null)
                                    {
                                        byte r = (byte)(colorObj["r"]?.Value<int>() ?? 0);
                                        byte g = (byte)(colorObj["g"]?.Value<int>() ?? 0);
                                        byte b = (byte)(colorObj["b"]?.Value<int>() ?? 0);
                                        overrideSettings.SetProjectionLineColor(new Color(r, g, b));
                                    }
                                }

                                if (Overrides["lineWeight"] != null)
                                {
                                    int lw = Overrides["lineWeight"].Value<int>();
                                    overrideSettings.SetProjectionLineWeight(lw);
                                }

                                if (Overrides["fillPattern"] != null)
                                {
                                    string fpName = Overrides["fillPattern"].Value<string>();
                                    FillPatternElement fpElem = new FilteredElementCollector(doc)
                                        .OfClass(typeof(FillPatternElement))
                                        .Cast<FillPatternElement>()
                                        .FirstOrDefault(fp => fp.Name == fpName);
                                    if (fpElem != null)
                                    {
                                        overrideSettings.SetSurfaceForegroundPatternId(fpElem.Id);
                                        overrideSettings.SetSurfaceForegroundPatternVisible(true);
                                    }
                                }

                                if (Overrides["halftone"] != null)
                                {
                                    overrideSettings.SetHalftone(Overrides["halftone"].Value<bool>());
                                }

                                view.SetFilterOverrides(filterId, overrideSettings);
                            }
                            break;

                        case "remove":
                            view.RemoveFilter(filterId);
                            break;
                    }

                    trans.Commit();

                    Result = new AIResult<bool>
                    {
                        Success = true,
                        Message = $"Filter '{FilterName}' {Action}ed successfully",
                        Response = true
                    };
                }
            }
            catch (Exception ex)
            {
                Result = new AIResult<bool>
                {
                    Success = false,
                    Message = $"Error managing view filters: {ex.Message}",
                    Response = false
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

        public string GetName() => "Manage View Filters";
    }
}
