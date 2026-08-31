using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services.Query
{
    public class QueryGeometryEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private Document Doc => uiApp.ActiveUIDocument.Document;
        private readonly ManualResetEvent _resetEvent = new(false);
        public int ElementId { get; private set; }
        public int? ViewId { get; private set; }
        public int? DetailLevel { get; private set; }
        public AIResult<object> Result { get; private set; }

        public void SetParameters(int elementId, int? viewId, int? detailLevel)
        {
            ElementId = elementId;
            ViewId = viewId;
            DetailLevel = detailLevel;
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
                    Result = new AIResult<object> { Success = false, Message = $"Element {ElementId} not found" };
                    return;
                }
                var options = new Options();
                if (ViewId.HasValue)
                    options.View = Doc.GetElement(new ElementId(ViewId.Value)) as View;
                if (DetailLevel.HasValue)
#if REVIT2026_OR_GREATER
                    options.DetailLevel = (ViewDetailLevel)DetailLevel.Value;
#elif REVIT2025_OR_GREATER
                    options.DetailLevel = (DetailLevel)DetailLevel.Value;
#else
                    options.DetailLevel = (ViewDetailLevel)DetailLevel.Value;
#endif
                options.ComputeReferences = true;

                var geom = element.get_Geometry(options);
                var solids = new List<object>();
                var boundingBox = element.get_BoundingBox(null);
                var boundingBoxData = boundingBox != null ? new
                {
                    Min = new { X = boundingBox.Min.X, Y = boundingBox.Min.Y, Z = boundingBox.Min.Z },
                    Max = new { X = boundingBox.Max.X, Y = boundingBox.Max.Y, Z = boundingBox.Max.Z }
                } : null;

                if (geom != null)
                {
                    CollectSolids(geom, solids);
                }

                var result = new
                {
                    ElementId = ElementId,
                    BoundingBox = boundingBoxData,
                    SolidCount = solids.Count,
                    Solids = solids
                };
                Result = new AIResult<object> { Success = true, Response = result };
            }
            catch (Exception ex)
            {
                Result = new AIResult<object> { Success = false, Message = ex.Message };
            }
            finally
            {
                _resetEvent.Set();
            }
        }

        private void CollectSolids(GeometryElement geomElement, List<object> solids)
        {
            foreach (var geomObj in geomElement)
            {
                if (geomObj is Solid solid && solid.Faces.Size > 0)
                {
                    var faceList = new List<object>();
                    foreach (Face face in solid.Faces)
                    {
                        faceList.Add(new
                        {
                            Area = face.Area,
                            SurfaceType = VersionCompat.GetSurfaceTypeName(face),
                            EdgeCount = face.EdgeLoops.Size
                        });
                    }
                    solids.Add(new
                    {
                        Volume = solid.Volume,
                        SurfaceArea = solid.SurfaceArea,
                        FaceCount = solid.Faces.Size,
                        Faces = faceList
                    });
                }
                if (geomObj is GeometryInstance instance)
                {
                    CollectSolids(instance.GetInstanceGeometry(), solids);
                }
            }
        }

        public bool WaitForCompletion(int timeout = 10000)
        {
            _resetEvent.Reset();
            return _resetEvent.WaitOne(timeout);
        }

        public string GetName() => "Query Geometry";
    }
}
