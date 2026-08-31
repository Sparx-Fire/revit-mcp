using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services.Views
{
    public class CreateCalloutEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private UIDocument uiDoc => uiApp.ActiveUIDocument;
        private Document doc => uiDoc.Document;

        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public string ViewName { get; private set; }
        public int HostViewId { get; private set; }
        public double MinX { get; private set; }
        public double MinY { get; private set; }
        public double MaxX { get; private set; }
        public double MaxY { get; private set; }

        public AIResult<int> Result { get; private set; }

        public void SetParameters(string name, int hostViewId, double minX, double minY, double maxX, double maxY)
        {
            ViewName = name;
            HostViewId = hostViewId;
            MinX = minX;
            MinY = minY;
            MaxX = maxX;
            MaxY = maxY;
            _resetEvent.Reset();
        }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;

            try
            {
                using (Transaction trans = new Transaction(doc, "Create Callout"))
                {
                    trans.Start();

                    View hostView = doc.GetElement(new ElementId(HostViewId)) as View;
                    if (hostView == null)
                    {
                        Result = new AIResult<int> { Success = false, Message = $"Host view with ID {HostViewId} not found" };
                        return;
                    }

                    ViewFamilyType vft = new FilteredElementCollector(doc)
                        .OfClass(typeof(ViewFamilyType))
                        .Cast<ViewFamilyType>()
                        .FirstOrDefault(vftype => vftype.ViewFamily == ViewFamily.Section);

                    if (vft == null)
                    {
                        Result = new AIResult<int> { Success = false, Message = "No section view family type found for callout" };
                        return;
                    }

                    XYZ minPt = new XYZ(MinX / 304.8, MinY / 304.8, 0);
                    XYZ maxPt = new XYZ(MaxX / 304.8, MaxY / 304.8, 0);

                    BoundingBoxXYZ bbox = new BoundingBoxXYZ
                    {
                        Min = minPt,
                        Max = maxPt
                    };

                    ViewSection callout = VersionCompat.CreateCallout(doc, hostView.Id, vft.Id, bbox);

                    if (callout == null)
                    {
                        Result = new AIResult<int>
                        {
                            Success = false,
                            Message = $"Callout view creation not supported in Revit 2026 via this API. Use an alternative view creation method."
                        };
                        return;
                    }

                    if (!string.IsNullOrEmpty(ViewName))
                    {
                        callout.Name = ViewName;
                    }

                    int calloutId = callout.Id.GetIntValue();

                    trans.Commit();

                    Result = new AIResult<int>
                    {
                        Success = true,
                        Message = $"Callout view '{callout.Name}' created successfully",
                        Response = calloutId
                    };
                }
            }
            catch (Exception ex)
            {
                Result = new AIResult<int>
                {
                    Success = false,
                    Message = $"Error creating callout: {ex.Message}"
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

        public string GetName() => "Create Callout";
    }
}
