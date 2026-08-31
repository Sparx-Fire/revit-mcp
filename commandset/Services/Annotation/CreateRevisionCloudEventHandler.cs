using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services.Annotation
{
    public class CreateRevisionCloudEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private UIDocument uiDoc => uiApp.ActiveUIDocument;
        private Document doc => uiDoc.Document;

        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public int RevisionId { get; private set; }
        public int ViewId { get; private set; }
        public List<JObject> Points { get; private set; }

        public AIResult<int> Result { get; private set; }

        public void SetParameters(int revisionId, int viewId, List<JObject> points)
        {
            RevisionId = revisionId;
            ViewId = viewId;
            Points = points;
            _resetEvent.Reset();
        }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;

            try
            {
                using (Transaction trans = new Transaction(doc, "Create Revision Cloud"))
                {
                    trans.Start();

                    ElementId revisionElemId = new ElementId(RevisionId);
                    ElementId viewElemId = new ElementId(ViewId);

                    View view = doc.GetElement(viewElemId) as View;
                    if (view == null)
                    {
                        Result = new AIResult<int> { Success = false, Message = $"View with ID {ViewId} not found" };
                        return;
                    }

                    CurveLoop curveLoop = new CurveLoop();
                    int count = Points.Count;
                    for (int i = 0; i < count; i++)
                    {
                        double x = Points[i]["x"]?.Value<double>() ?? 0;
                        double y = Points[i]["y"]?.Value<double>() ?? 0;
                        XYZ startPt = new XYZ(x / 304.8, y / 304.8, 0);

                        JObject nextPt = Points[(i + 1) % count];
                        double nx = nextPt["x"]?.Value<double>() ?? 0;
                        double ny = nextPt["y"]?.Value<double>() ?? 0;
                        XYZ endPt = new XYZ(nx / 304.8, ny / 304.8, 0);

                        curveLoop.Append(Line.CreateBound(startPt, endPt));
                    }

                    IList<CurveLoop> loops = new List<CurveLoop> { curveLoop };

                    RevisionCloud cloud = VersionCompat.CreateRevisionCloud(doc, revisionElemId, loops, viewElemId);

                    int cloudId = cloud.Id.GetIntValue();

                    trans.Commit();

                    Result = new AIResult<int>
                    {
                        Success = true,
                        Message = "Revision cloud created successfully",
                        Response = cloudId
                    };
                }
            }
            catch (Exception ex)
            {
                Result = new AIResult<int>
                {
                    Success = false,
                    Message = $"Error creating revision cloud: {ex.Message}"
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

        public string GetName() => "Create Revision Cloud";
    }
}
