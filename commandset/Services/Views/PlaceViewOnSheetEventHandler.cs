using RevitMCPCommandSet.Models.Views;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services.Views
{
    public class PlaceViewOnSheetEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private UIDocument uiDoc => uiApp.ActiveUIDocument;
        private Document doc => uiDoc.Document;

        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public List<ViewportCreationInfo> CreatedInfo { get; private set; }

        public AIResult<List<int>> Result { get; private set; }

        private List<string> _warnings = new List<string>();

        public void SetParameters(List<ViewportCreationInfo> data)
        {
            CreatedInfo = data;
            _resetEvent.Reset();
        }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;

            try
            {
                var viewportIds = new List<int>();
                _warnings.Clear();

                foreach (var info in CreatedInfo)
                {
                    using (Transaction trans = new Transaction(doc, "Place Viewport"))
                    {
                        trans.Start();

                        ElementId sheetId = new ElementId(info.SheetId);
                        ViewSheet sheet = doc.GetElement(sheetId) as ViewSheet;
                        if (sheet == null)
                        {
                            _warnings.Add($"Sheet with ID {info.SheetId} not found.");
                            trans.Commit();
                            continue;
                        }

                        ElementId viewId = new ElementId(info.ViewId);
                        View view = doc.GetElement(viewId) as View;
                        if (view == null)
                        {
                            _warnings.Add($"View with ID {info.ViewId} not found.");
                            trans.Commit();
                            continue;
                        }

                        XYZ location = new XYZ(
                            info.PositionX / 304.8,
                            info.PositionY / 304.8,
                            0
                        );

                        Viewport viewport = Viewport.Create(doc, sheetId, viewId, location);

                        if (viewport != null)
                        {
                            if (info.ViewportTypeId > 0)
                            {
                                Element vpTypeElem = doc.GetElement(new ElementId(info.ViewportTypeId));
                                if (vpTypeElem != null)
                                {
                                    viewport.ChangeTypeId(vpTypeElem.Id);
                                }
                            }

                            if (info.DisplayTitle.HasValue)
                            {
                                viewport.get_Parameter(BuiltInParameter.VIEWPORT_DETAIL_NUMBER)?.Set(info.DisplayTitle.Value ? 0 : 1);
                            }

                            if (info.ScaleOverride > 0)
                            {
                                try
                                {
                                    viewport.get_Parameter(BuiltInParameter.VIEW_SCALE)?.Set(info.ScaleOverride);
                                }
                                catch
                                {
                                    _warnings.Add($"Could not override scale to {info.ScaleOverride}.");
                                }
                            }

                            if (info.Rotation > 0)
                            {
                                try
                                {
#if REVIT2026_OR_GREATER
                                    // R26: VIEWPORT_VIEW_ROTATION removed
                                    Parameter rotParam = viewport.LookupParameter("Rotation on Sheet");
                                    rotParam?.Set(info.Rotation);
#elif REVIT2025_OR_GREATER
                                    viewport.get_Parameter(BuiltInParameter.VIEWPORT_VIEW_ROTATION)?.Set(info.Rotation);
#endif
                                }
                                catch
                                {
                                    _warnings.Add($"Could not set rotation to {info.Rotation}.");
                                }
                            }

                            foreach (var param in info.Parameters)
                            {
                                Parameter vpParam = viewport.LookupParameter(param.Key);
                                if (vpParam != null)
                                {
                                    SetParameterValue(vpParam, param.Value);
                                }
                            }

                            viewportIds.Add(viewport.Id.GetIntValue());
                        }

                        trans.Commit();
                    }
                }

                string message = $"Successfully placed {viewportIds.Count} viewport(s) on sheets.";
                if (_warnings.Count > 0)
                {
                    message += "\n\nWarnings:\n  - " + string.Join("\n  - ", _warnings);
                }

                Result = new AIResult<List<int>>
                {
                    Success = true,
                    Message = message,
                    Response = viewportIds,
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<List<int>>
                {
                    Success = false,
                    Message = $"Error placing view on sheet: {ex.Message}",
                };
            }
            finally
            {
                _resetEvent.Set();
            }
        }

        private void SetParameterValue(Parameter param, object value)
        {
            if (value == null) return;

            switch (param.StorageType)
            {
                case StorageType.Integer:
                    if (value is long l) param.Set((int)l);
                    else if (value is int i) param.Set(i);
                    else if (value is bool b) param.Set(b ? 1 : 0);
                    break;
                case StorageType.Double:
                    if (value is double d) param.Set(d);
                    else if (value is long ld) param.Set((double)ld);
                    break;
                case StorageType.String:
                    param.Set(value.ToString());
                    break;
            }
        }

        public bool WaitForCompletion(int timeoutMilliseconds = 15000)
        {
            _resetEvent.Reset();
            return _resetEvent.WaitOne(timeoutMilliseconds);
        }

        public string GetName() => "Place Viewport";
    }
}
