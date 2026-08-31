using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Models.Common;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services.Modify
{
    public class SetElementCurveEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private Document Doc => uiApp.ActiveUIDocument.Document;
        private readonly ManualResetEvent _resetEvent = new(false);
        public int ElementId { get; private set; }
        public JObject StartPoint { get; private set; }
        public JObject EndPoint { get; private set; }
        public AIResult<bool> Result { get; private set; }

        public void SetParameters(int elementId, JObject startPoint, JObject endPoint)
        {
            ElementId = elementId;
            StartPoint = startPoint;
            EndPoint = endPoint;
            _resetEvent.Reset();
        }

        public void Execute(UIApplication app)
        {
            uiApp = app;
            try
            {
                var element = Doc.GetElement(new ElementId(ElementId));
                if (element == null)
                {
                    Result = new AIResult<bool> { Success = false, Message = $"Element {ElementId} not found" };
                    return;
                }
                var location = element.Location as LocationCurve;
                if (location == null)
                {
                    Result = new AIResult<bool> { Success = false, Message = "Element does not have a LocationCurve" };
                    return;
                }
                var p0 = new XYZ(
                    StartPoint["x"]?.Value<double>() ?? 0,
                    StartPoint["y"]?.Value<double>() ?? 0,
                    StartPoint["z"]?.Value<double>() ?? 0
                );
                var p1 = new XYZ(
                    EndPoint["x"]?.Value<double>() ?? 0,
                    EndPoint["y"]?.Value<double>() ?? 0,
                    EndPoint["z"]?.Value<double>() ?? 0
                );
                using (var trans = new Transaction(Doc, "Set Element Curve"))
                {
                    trans.Start();
                    location.Curve = Line.CreateBound(p0, p1);
                    trans.Commit();
                }
                Result = new AIResult<bool> { Success = true, Response = true };
            }
            catch (Exception ex)
            {
                Result = new AIResult<bool> { Success = false, Message = ex.Message };
            }
            finally
            {
                _resetEvent.Set();
            }
        }

        public bool WaitForCompletion(int timeout = 10000)
        {
            _resetEvent.Reset();
            return _resetEvent.WaitOne(timeout);
        }

        public string GetName() => "Set Element Curve";
    }
}
