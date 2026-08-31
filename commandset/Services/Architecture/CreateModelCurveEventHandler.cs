using RevitMCPCommandSet.Models.Architecture;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services.Architecture
{
    public class CreateModelCurveEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication _uiApp;
        private UIDocument _uiDoc => _uiApp.ActiveUIDocument;
        private Document _doc => _uiDoc.Document;

        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public List<ModelCurveCreationInfo> CurveData { get; private set; }

        public AIResult<List<int>> Result { get; private set; }

        private List<string> _warnings = new List<string>();

        public void SetParameters(List<ModelCurveCreationInfo> data)
        {
            CurveData = data;
            _resetEvent.Reset();
        }

        public void Execute(UIApplication uiapp)
        {
            _uiApp = uiapp;

            try
            {
                var elementIds = new List<int>();
                _warnings.Clear();

                foreach (var info in CurveData)
                {
                    using (Transaction tx = new Transaction(_doc, "Create Model Curve"))
                    {
                        tx.Start();

                        try
                        {
                            Curve curve = null;
                            SketchPlane sketchPlane = null;

                            // Build curve based on type
                            switch (info.CurveType?.ToLower())
                            {
                                case "line":
                                    if (info.Points != null && info.Points.Count >= 2)
                                    {
                                        XYZ start = new XYZ(info.Points[0].X / 304.8, info.Points[0].Y / 304.8, info.Points[0].Z / 304.8);
                                        XYZ end = new XYZ(info.Points[1].X / 304.8, info.Points[1].Y / 304.8, info.Points[1].Z / 304.8);
                                        curve = Line.CreateBound(start, end);
                                    }
                                    break;

                                case "arc":
                                    if (info.Points != null && info.Points.Count >= 3)
                                    {
                                        XYZ p0 = new XYZ(info.Points[0].X / 304.8, info.Points[0].Y / 304.8, info.Points[0].Z / 304.8);
                                        XYZ p1 = new XYZ(info.Points[1].X / 304.8, info.Points[1].Y / 304.8, info.Points[1].Z / 304.8);
                                        XYZ p2 = new XYZ(info.Points[2].X / 304.8, info.Points[2].Y / 304.8, info.Points[2].Z / 304.8);
                                        curve = Arc.Create(p0, p1, p2);
                                    }
                                    break;

                                case "circle":
                                    if (info.Center != null && info.Radius.HasValue)
                                    {
                                        XYZ center = new XYZ(info.Center.X / 304.8, info.Center.Y / 304.8, info.Center.Z / 304.8);
                                        double radius = info.Radius.Value / 304.8;
                                        XYZ normal = info.Normal != null
                                            ? new XYZ(info.Normal.X, info.Normal.Y, info.Normal.Z)
                                            : XYZ.BasisZ;
                                        curve = Arc.Create(center, radius, 0, 2 * Math.PI, normal, normal.CrossProduct(XYZ.BasisX ?? XYZ.BasisY));
                                    }
                                    break;

                                case "spline":
                                    if (info.Points != null && info.Points.Count >= 2)
                                    {
                                        IList<XYZ> pts = info.Points.Select(p => new XYZ(p.X / 304.8, p.Y / 304.8, p.Z / 304.8)).ToList();
                                        curve = VersionCompat.CreateNurbSpline(pts);
                                    }
                                    break;
                            }

                            if (curve == null)
                            {
                                _warnings.Add("Could not create curve from provided parameters");
                                continue;
                            }

                            // Get sketch plane
                            if (info.SketchPlaneId > 0)
                            {
                                sketchPlane = _doc.GetElement(new ElementId(info.SketchPlaneId)) as SketchPlane;
                            }

                            if (sketchPlane == null)
                            {
                                // Create sketch plane from curve's plane
                                XYZ origin = curve.Evaluate(0.0, true);
                                XYZ normal = XYZ.BasisZ;
                                if (curve is Line line)
                                {
                                    normal = line.Direction.CrossProduct(XYZ.BasisX).GetLength() > 0.001
                                        ? line.Direction.CrossProduct(XYZ.BasisX).Normalize()
                                        : XYZ.BasisZ;
                                }
                                else
                                {
                                    Transform t = curve.ComputeDerivatives(0.0, false);
                                    normal = t.BasisZ.Normalize();
                                }

                                Plane plane = Plane.CreateByNormalAndOrigin(normal, origin);
                                sketchPlane = SketchPlane.Create(_doc, plane);
                            }

                            ModelCurve modelCurve = _doc.IsFamilyDocument
                                ? _doc.FamilyCreate.NewModelCurve(curve, sketchPlane)
                                : _doc.Create.NewModelCurve(curve, sketchPlane);

                            if (modelCurve != null)
                            {
                                elementIds.Add(modelCurve.Id.GetIntValue());
                            }

                            tx.Commit();
                        }
                        catch (Exception ex)
                        {
                            tx.RollBack();
                            _warnings.Add($"Failed to create model curve: {ex.Message}");
                        }
                    }
                }

                string message = $"Successfully created {elementIds.Count} model curve(s)";
                if (_warnings.Count > 0)
                {
                    message += "\nWarnings:\n  " + string.Join("\n  ", _warnings);
                }

                Result = new AIResult<List<int>>
                {
                    Success = true,
                    Message = message,
                    Response = elementIds
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<List<int>>
                {
                    Success = false,
                    Message = $"Error creating model curves: {ex.Message}",
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

        public string GetName()
        {
            return "Create Model Curve";
        }
    }
}
