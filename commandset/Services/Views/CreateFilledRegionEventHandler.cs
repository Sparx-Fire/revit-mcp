using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services.Views
{
    public class CreateFilledRegionEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private UIDocument uiDoc => uiApp.ActiveUIDocument;
        private Document doc => uiDoc.Document;

        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public int ViewId { get; private set; }
        public List<List<JObject>> Boundary { get; private set; }
        public string FilledRegionTypeName { get; private set; }

        public AIResult<int> Result { get; private set; }

        public void SetParameters(int viewId, List<List<JObject>> boundary, string filledRegionTypeName)
        {
            ViewId = viewId;
            Boundary = boundary;
            FilledRegionTypeName = filledRegionTypeName;
            _resetEvent.Reset();
        }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;

            try
            {
                using (Transaction trans = new Transaction(doc, "Create Filled Region"))
                {
                    trans.Start();

                    View view = doc.GetElement(new ElementId(ViewId)) as View;
                    if (view == null)
                    {
                        Result = new AIResult<int> { Success = false, Message = $"View with ID {ViewId} not found" };
                        return;
                    }

                    FilledRegionType regionType = null;
                    if (!string.IsNullOrEmpty(FilledRegionTypeName))
                    {
                        regionType = new FilteredElementCollector(doc)
                            .OfClass(typeof(FilledRegionType))
                            .Cast<FilledRegionType>()
                            .FirstOrDefault(ft => ft.Name == FilledRegionTypeName);
                    }

                    if (regionType == null)
                    {
                        regionType = new FilteredElementCollector(doc)
                            .OfClass(typeof(FilledRegionType))
                            .Cast<FilledRegionType>()
                            .FirstOrDefault();
                    }

                    if (regionType == null)
                    {
                        Result = new AIResult<int> { Success = false, Message = "No filled region type found" };
                        return;
                    }

                    CurveLoop curveLoop = new CurveLoop();
                    if (Boundary.Count > 0)
                    {
                        List<JObject> points = Boundary[0];
                        int count = points.Count;
                        for (int i = 0; i < count; i++)
                        {
                            double x = points[i]["x"]?.Value<double>() ?? 0;
                            double y = points[i]["y"]?.Value<double>() ?? 0;
                            XYZ startPt = new XYZ(x / 304.8, y / 304.8, 0);

                            JObject nextPt = points[(i + 1) % count];
                            double nx = nextPt["x"]?.Value<double>() ?? 0;
                            double ny = nextPt["y"]?.Value<double>() ?? 0;
                            XYZ endPt = new XYZ(nx / 304.8, ny / 304.8, 0);

                            curveLoop.Append(Line.CreateBound(startPt, endPt));
                        }
                    }

                    IList<CurveLoop> loops = new List<CurveLoop> { curveLoop };
                    FilledRegion filledRegion = FilledRegion.Create(doc, regionType.Id, view.Id, loops);

                    int regionId = filledRegion.Id.GetIntValue();

                    trans.Commit();

                    Result = new AIResult<int>
                    {
                        Success = true,
                        Message = "Filled region created successfully",
                        Response = regionId
                    };
                }
            }
            catch (Exception ex)
            {
                Result = new AIResult<int>
                {
                    Success = false,
                    Message = $"Error creating filled region: {ex.Message}"
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

        public string GetName() => "Create Filled Region";
    }
}
