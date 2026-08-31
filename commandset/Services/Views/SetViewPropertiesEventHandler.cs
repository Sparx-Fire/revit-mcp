using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services.Views
{
    public class SetViewPropertiesEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private UIDocument uiDoc => uiApp.ActiveUIDocument;
        private Document doc => uiDoc.Document;

        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public int ViewId { get; private set; }
        public JObject Properties { get; private set; }

        public AIResult<bool> Result { get; private set; }

        public void SetParameters(int viewId, JObject properties)
        {
            ViewId = viewId;
            Properties = properties;
            _resetEvent.Reset();
        }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;

            try
            {
                using (Transaction trans = new Transaction(doc, "Set View Properties"))
                {
                    trans.Start();

                    View view = doc.GetElement(new ElementId(ViewId)) as View;
                    if (view == null)
                    {
                        Result = new AIResult<bool> { Success = false, Message = $"View with ID {ViewId} not found" };
                        return;
                    }

                    if (Properties == null)
                    {
                        Result = new AIResult<bool> { Success = false, Message = "No properties provided" };
                        return;
                    }

                    if (Properties["scale"] != null)
                    {
                        int scaleVal = Properties["scale"].Value<int>();
                        if (view is ViewPlan || view is ViewSection || view is View3D)
                        {
                            view.Scale = scaleVal;
                        }
                        else
                        {
                            view.get_Parameter(BuiltInParameter.VIEW_SCALE)?.Set(scaleVal);
                        }
                    }

                    if (Properties["detailLevel"] != null)
                    {
                        string dl = Properties["detailLevel"].Value<string>().ToLowerInvariant();
                        switch (dl)
                        {
                            case "coarse":
                                view.DetailLevel = ViewDetailLevel.Coarse;
                                break;
                            case "medium":
                                view.DetailLevel = ViewDetailLevel.Medium;
                                break;
                            case "fine":
                                view.DetailLevel = ViewDetailLevel.Fine;
                                break;
                        }
                    }

                    if (Properties["displayStyle"] != null)
                    {
                        string ds = Properties["displayStyle"].Value<string>().ToLowerInvariant();
                        int styleValue = 0;
                        switch (ds)
                        {
                            case "wireframe": styleValue = 0; break;
                            case "hidden":
                            case "hiddenline": styleValue = 1; break;
                            case "shaded":
                            case "shading": styleValue = 2; break;
                            case "consistent_colors": styleValue = 3; break;
                            case "realistic": styleValue = 4; break;
                        }
#if REVIT2026_OR_GREATER
                        view.get_Parameter(BuiltInParameter.MODEL_GRAPHICS_STYLE)?.Set(styleValue);
#else
                        // R20-R25: use VIEW_DISPLAY_STYLE or MODEL_GRAPHICS_STYLE
                        view.get_Parameter(BuiltInParameter.MODEL_GRAPHICS_STYLE)?.Set(styleValue);
#endif
                    }

                    if (Properties["templateId"] != null)
                    {
                        int templateIdVal = Properties["templateId"].Value<int>();
                        ElementId templateId = new ElementId(templateIdVal);
                        View templateView = doc.GetElement(templateId) as View;
                        if (templateView != null && templateView.IsTemplate)
                        {
                            view.ViewTemplateId = templateId;
                        }
                    }

                    if (Properties["cropBox"] != null)
                    {
                        JObject cropBox = Properties["cropBox"] as JObject;
                        if (cropBox != null)
                        {
                            double minX = cropBox["minX"]?.Value<double>() ?? 0;
                            double minY = cropBox["minY"]?.Value<double>() ?? 0;
                            double maxX = cropBox["maxX"]?.Value<double>() ?? 10;
                            double maxY = cropBox["maxY"]?.Value<double>() ?? 10;

                            view.CropBox = new BoundingBoxXYZ
                            {
                                Min = new XYZ(minX / 304.8, minY / 304.8, -10),
                                Max = new XYZ(maxX / 304.8, maxY / 304.8, 10)
                            };
                            view.CropBoxActive = true;
                            view.CropBoxVisible = true;
                        }
                    }

                    trans.Commit();

                    Result = new AIResult<bool>
                    {
                        Success = true,
                        Message = "View properties updated successfully",
                        Response = true
                    };
                }
            }
            catch (Exception ex)
            {
                Result = new AIResult<bool>
                {
                    Success = false,
                    Message = $"Error setting view properties: {ex.Message}",
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

        public string GetName() => "Set View Properties";
    }
}
