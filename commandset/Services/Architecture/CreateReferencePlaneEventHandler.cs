using RevitMCPCommandSet.Models.Architecture;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services.Architecture
{
    public class CreateReferencePlaneEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication _uiApp;
        private UIDocument _uiDoc => _uiApp.ActiveUIDocument;
        private Document _doc => _uiDoc.Document;

        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public List<ReferencePlaneCreationInfo> PlaneData { get; private set; }

        public AIResult<List<int>> Result { get; private set; }

        private List<string> _warnings = new List<string>();

        public void SetParameters(List<ReferencePlaneCreationInfo> data)
        {
            PlaneData = data;
            _resetEvent.Reset();
        }

        public void Execute(UIApplication uiapp)
        {
            _uiApp = uiapp;

            try
            {
                var elementIds = new List<int>();
                _warnings.Clear();

                foreach (var info in PlaneData)
                {
                    using (Transaction tx = new Transaction(_doc, "Create Reference Plane"))
                    {
                        tx.Start();

                        try
                        {
                            ReferencePlane refPlane = null;

                            switch (info.CreationMethod?.ToLower())
                            {
                                case "bystartend":
                                case "byline":
                                    // Create reference plane by bubble end and free end
                                    if (info.BubbleEnd != null && info.FreeEnd != null)
                                    {
                                        XYZ bubbleEnd = new XYZ(info.BubbleEnd.X / 304.8, info.BubbleEnd.Y / 304.8, info.BubbleEnd.Z / 304.8);
                                        XYZ freeEnd = new XYZ(info.FreeEnd.X / 304.8, info.FreeEnd.Y / 304.8, info.FreeEnd.Z / 304.8);
                                        XYZ normal = info.Normal != null
                                            ? new XYZ(info.Normal.X, info.Normal.Y, info.Normal.Z)
                                            : XYZ.BasisZ;

                                        // Get a suitable view for the reference plane
                                        View view = _doc.ActiveView;
                                        refPlane = _doc.IsFamilyDocument
                                            ? _doc.FamilyCreate.NewReferencePlane(bubbleEnd, freeEnd, normal, view)
                                            : _doc.Create.NewReferencePlane(bubbleEnd, freeEnd, normal, view);
                                    }
                                    break;

                                case "bynormal":
                                    // Create reference plane by origin and normal
                                    if (info.Origin != null && info.Normal != null)
                                    {
                                        XYZ origin = new XYZ(info.Origin.X / 304.8, info.Origin.Y / 304.8, info.Origin.Z / 304.8);
                                        XYZ normal = new XYZ(info.Normal.X, info.Normal.Y, info.Normal.Z);
                                        Plane plane = Plane.CreateByNormalAndOrigin(normal, origin);
                                        refPlane = _doc.IsFamilyDocument
                                            ? _doc.FamilyCreate.NewReferencePlane(plane.Origin, plane.Origin + plane.XVec, plane.Normal, _doc.ActiveView)
                                            : VersionCompat.CreateReferencePlane(_doc, plane);
                                    }
                                    break;

                                case "bypoints":
                                    // Create reference plane from three points
                                    if (info.Points != null && info.Points.Count >= 3)
                                    {
                                        XYZ p1 = new XYZ(info.Points[0].X / 304.8, info.Points[0].Y / 304.8, info.Points[0].Z / 304.8);
                                        XYZ p2 = new XYZ(info.Points[1].X / 304.8, info.Points[1].Y / 304.8, info.Points[1].Z / 304.8);
                                        XYZ p3 = new XYZ(info.Points[2].X / 304.8, info.Points[2].Y / 304.8, info.Points[2].Z / 304.8);

                                        XYZ v1 = p2.Subtract(p1);
                                        XYZ v2 = p3.Subtract(p1);
                                        XYZ normal = v1.CrossProduct(v2).Normalize();

                                        View view = _doc.ActiveView;
                                        refPlane = _doc.IsFamilyDocument
                                            ? _doc.FamilyCreate.NewReferencePlane(p1, p2, normal, view)
                                            : _doc.Create.NewReferencePlane(p1, p2, normal, view);
                                    }
                                    break;

                                default:
                                    _warnings.Add($"Unknown creation method: {info.CreationMethod}");
                                    continue;
                            }

                            if (refPlane == null)
                            {
                                _warnings.Add("Failed to create reference plane");
                                continue;
                            }

                            // Set name if provided
                            if (!string.IsNullOrEmpty(info.Name))
                            {
                                refPlane.Name = info.Name;
                            }

                            elementIds.Add(refPlane.Id.GetIntValue());

                            tx.Commit();
                        }
                        catch (Exception ex)
                        {
                            tx.RollBack();
                            _warnings.Add($"Failed to create reference plane: {ex.Message}");
                        }
                    }
                }

                string message = $"Successfully created {elementIds.Count} reference plane(s)";
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
                    Message = $"Error creating reference planes: {ex.Message}",
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
            return "Create Reference Plane";
        }
    }
}
