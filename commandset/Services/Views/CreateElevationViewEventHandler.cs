using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services.Views
{
    public class CreateElevationViewEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private UIDocument uiDoc => uiApp.ActiveUIDocument;
        private Document doc => uiDoc.Document;

        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public string ViewName { get; private set; }
        public int DirectionIndex { get; private set; }
        public string ViewFamilyTypeName { get; private set; }

        public AIResult<int> Result { get; private set; }

        public void SetParameters(string name, int directionIndex, string viewFamilyTypeName)
        {
            ViewName = name;
            DirectionIndex = directionIndex;
            ViewFamilyTypeName = viewFamilyTypeName;
            _resetEvent.Reset();
        }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;

            try
            {
                using (Transaction trans = new Transaction(doc, "Create Elevation View"))
                {
                    trans.Start();

                    ViewFamilyType vft = new FilteredElementCollector(doc)
                        .OfClass(typeof(ViewFamilyType))
                        .Cast<ViewFamilyType>()
                        .FirstOrDefault(vftype =>
                            vftype.ViewFamily == ViewFamily.Elevation &&
                            (string.IsNullOrEmpty(ViewFamilyTypeName) || vftype.Name == ViewFamilyTypeName));

                    if (vft == null)
                    {
                        vft = new FilteredElementCollector(doc)
                            .OfClass(typeof(ViewFamilyType))
                            .Cast<ViewFamilyType>()
                            .FirstOrDefault(vftype => vftype.ViewFamily == ViewFamily.Elevation);
                    }

                    if (vft == null)
                    {
                        Result = new AIResult<int> { Success = false, Message = "No elevation view family type found" };
                        return;
                    }

                    Level level = new FilteredElementCollector(doc)
                        .OfClass(typeof(Level))
                        .Cast<Level>()
                        .FirstOrDefault();

                    if (level == null)
                    {
                        Result = new AIResult<int> { Success = false, Message = "No level found in project" };
                        return;
                    }

#if REVIT2026_OR_GREATER
                    // R26: CreateElevationMarker(Document, ElementId, XYZ, int)
                    ElevationMarker marker = ElevationMarker.CreateElevationMarker(doc, vft.Id, new XYZ(0, 0, level.Elevation), 100);
#elif REVIT2025_OR_GREATER
                    ElevationMarker marker = ElevationMarker.CreateElevationMarker(doc, vft.Id, level.Id, new XYZ(0, 0, level.Elevation));
#else
                    // R20-R21: Elevation creation not supported via this API
                    Result = new AIResult<int> { Success = false, Message = "Elevation view creation not supported in Revit 2020-2021" };
                    trans.RollBack();
                    return;
#endif

#if REVIT2025_OR_GREATER
                    int dirIndex = Math.Max(0, Math.Min(3, DirectionIndex));
                    ViewSection elevationView = VersionCompat.CreateElevationView(marker, level.Id, dirIndex);

                    if (elevationView == null)
                    {
                        Result = new AIResult<int>
                        {
                            Success = false,
                            Message = $"Elevation view creation from marker not supported in Revit 2026 via this API. Use create_view tool with 'Elevation' viewType instead."
                        };
                        return;
                    }

                    if (!string.IsNullOrEmpty(ViewName))
                    {
                        elevationView.Name = ViewName;
                    }

                    int viewId = elevationView.Id.GetIntValue();

                    trans.Commit();

                    Result = new AIResult<int>
                    {
                        Success = true,
                        Message = $"Elevation view '{elevationView.Name}' created successfully at direction index {dirIndex}",
                        Response = viewId
                    };
#endif
                }
            }
            catch (Exception ex)
            {
                Result = new AIResult<int>
                {
                    Success = false,
                    Message = $"Error creating elevation view: {ex.Message}"
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

        public string GetName() => "Create Elevation View";
    }
}
