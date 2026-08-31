using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services.Modify
{
    public class ManageGraphicsResourcesEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private UIDocument uiDoc => uiApp.ActiveUIDocument;
        private Document doc => uiDoc.Document;

        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public string Action { get; private set; }
        public string ResourceName { get; private set; }
        public JObject Properties { get; private set; }

        public AIResult<bool> Result { get; private set; }
        private List<string> _warnings = new List<string>();

        public void SetParameters(string action, string name, JObject properties)
        {
            Action = action;
            ResourceName = name;
            Properties = properties;
            _resetEvent.Reset();
        }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;

            try
            {
                using (Transaction trans = new Transaction(doc, "Manage Graphics Resources"))
                {
                    trans.Start();

                    switch (Action.ToLowerInvariant())
                    {
                        case "line_style":
                            HandleLineStyle();
                            break;
                        case "fill_pattern":
                            HandleFillPattern();
                            break;
                        default:
                            Result = new AIResult<bool> { Success = false, Message = $"Unknown action: {Action}. Use 'line_style' or 'fill_pattern'" };
                            return;
                    }

                    trans.Commit();

                    Result = new AIResult<bool>
                    {
                        Success = true,
                        Message = $"Graphics resource '{Action}' managed successfully",
                        Response = true
                    };
                }
            }
            catch (Exception ex)
            {
                Result = new AIResult<bool>
                {
                    Success = false,
                    Message = $"Error managing graphics resources: {ex.Message}",
                    Response = false
                };
            }
            finally
            {
                _resetEvent.Set();
            }
        }

        private void HandleLineStyle()
        {
            GraphicsStyle existingStyle = new FilteredElementCollector(doc)
                .OfClass(typeof(GraphicsStyle))
                .Cast<GraphicsStyle>()
                .FirstOrDefault(gs => gs.Name == ResourceName);

            if (existingStyle != null && Properties != null)
            {
                if (Properties["lineWeight"] != null)
                {
                    int lineWeight = Properties["lineWeight"].Value<int>();
#if REVIT2026_OR_GREATER
                    // R26: GraphicsStyleCategory parameter access changed
                    _warnings.Add("Line weight update not supported in Revit 2026");
#elif REVIT2025_OR_GREATER
                    existingStyle.GraphicsStyleCategory?
                        .get_Parameter(BuiltInParameter.LINE_WEIGHT_PROJECTION)?.Set(lineWeight);
#else
                    // R20-R23: Category.Parameters not available, skip
                    _warnings.Add("Line weight update not supported in this Revit version");
#endif
                }

                if (Properties["color"] != null)
                {
                    JObject colorObj = Properties["color"] as JObject;
                    if (colorObj != null)
                    {
                        byte r = (byte)(colorObj["r"]?.Value<int>() ?? 0);
                        byte g = (byte)(colorObj["g"]?.Value<int>() ?? 0);
                        byte b = (byte)(colorObj["b"]?.Value<int>() ?? 0);
                        Color color = new Color(r, g, b);
#if REVIT2026_OR_GREATER
                        // R26: GraphicsStyleCategory parameter access changed
                        _warnings.Add("Line color update not supported in Revit 2026");
#elif REVIT2025_OR_GREATER
                        existingStyle.GraphicsStyleCategory?
                            .get_Parameter(BuiltInParameter.LINE_COLOR)?.Set(color);
#else
                        // R20-R24: Category.Parameters not available, skip
                        _warnings.Add("Line color update not supported in this Revit version");
#endif
                    }
                }

                if (Properties["linePattern"] != null)
                {
                    string patternName = Properties["linePattern"].Value<string>();
                    LinePatternElement pattern = new FilteredElementCollector(doc)
                        .OfClass(typeof(LinePatternElement))
                        .Cast<LinePatternElement>()
                        .FirstOrDefault(lp => lp.Name == patternName);

                    if (pattern != null)
                    {
#if REVIT2026_OR_GREATER
                        // R26: GraphicsStyleCategory parameter access changed
                        _warnings.Add("Line pattern update not supported in Revit 2026");
#elif REVIT2025_OR_GREATER
                        existingStyle.GraphicsStyleCategory?
                            .get_Parameter(BuiltInParameter.LINE_PATTERN)?.Set(pattern.Id.GetIntValue());
#else
                        // R20-R24: Category.Parameters not available, skip
                        _warnings.Add("Line pattern update not supported in this Revit version");
#endif
                    }
                }
            }
        }

        private void HandleFillPattern()
        {
            FillPatternElement existingPattern = new FilteredElementCollector(doc)
                .OfClass(typeof(FillPatternElement))
                .Cast<FillPatternElement>()
                .FirstOrDefault(fp => fp.Name == ResourceName);

            if (existingPattern != null && Properties != null)
            {
                if (Properties["color"] != null)
                {
                    JObject colorObj = Properties["color"] as JObject;
                    if (colorObj != null)
                    {
                        int red = colorObj["r"]?.Value<int>() ?? 0;
                        int green = colorObj["g"]?.Value<int>() ?? 0;
                        int blue = colorObj["b"]?.Value<int>() ?? 0;

                        FillPattern fillPattern = existingPattern.GetFillPattern();
#if REVIT2026_OR_GREATER
                        // R26: FillPattern.Color removed
                        _warnings.Add("Fill pattern color update not supported in Revit 2026");
#elif REVIT2025_OR_GREATER
                        fillPattern.Color = red + green * 256 + blue * 65536;
                        existingPattern.SetFillPattern(fillPattern);
#else
                        // R20-R23: FillPattern.Color not available, skip color update
                        _warnings.Add("Fill pattern color update not supported in this Revit version");
#endif
                    }
                }
            }
        }

        public bool WaitForCompletion(int timeoutMilliseconds = 10000)
        {
            _resetEvent.Reset();
            return _resetEvent.WaitOne(timeoutMilliseconds);
        }

        public string GetName() => "Manage Graphics Resources";
    }
}
