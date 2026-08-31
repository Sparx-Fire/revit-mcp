using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services.Query
{
    public class QueryReferencesEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private Document Doc => uiApp.ActiveUIDocument.Document;
        private readonly ManualResetEvent _resetEvent = new(false);
        public int ElementId { get; private set; }
        public AIResult<List<object>> Result { get; private set; }

        public void SetParameters(int elementId)
        {
            ElementId = elementId;
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
                    Result = new AIResult<List<object>> { Success = false, Message = $"Element {ElementId} not found" };
                    return;
                }
                var options = new Options { ComputeReferences = true, DetailLevel = ViewDetailLevel.Fine };
                var geom = element.get_Geometry(options);
                var references = new List<object>();
                if (geom != null)
                    CollectReferences(geom, references);
                Result = new AIResult<List<object>> { Success = true, Response = references };
            }
            catch (Exception ex)
            {
                Result = new AIResult<List<object>> { Success = false, Message = ex.Message };
            }
            finally
            {
                _resetEvent.Set();
            }
        }

        private void CollectReferences(GeometryElement geomElement, List<object> references)
        {
            foreach (var geomObj in geomElement)
            {
                if (geomObj is Solid solid)
                {
                    foreach (Face face in solid.Faces)
                    {
                        if (face.Reference != null)
                        {
                            references.Add(new
                            {
                                Type = "Face",
                                Reference = face.Reference.ConvertToStableRepresentation(Doc),
                                Area = face.Area,
                                SurfaceType = VersionCompat.GetSurfaceTypeName(face)
                            });
                        }
                        foreach (EdgeArray edgeArray in face.EdgeLoops)
                        {
                            foreach (Edge edge in edgeArray)
                            {
                                if (edge.Reference != null)
                                {
                                    references.Add(new
                                    {
                                        Type = "Edge",
                                        Reference = edge.Reference.ConvertToStableRepresentation(Doc),
                                        Length = edge.ApproximateLength,
                                        CurveType = edge.AsCurve()?.GetType().Name
                                    });
                                }
                            }
                        }
                    }
                }
                if (geomObj is GeometryInstance instance)
                {
                    CollectReferences(instance.GetInstanceGeometry(), references);
                    CollectReferences(instance.GetSymbolGeometry(), references);
                }
            }
        }

        public bool WaitForCompletion(int timeout = 10000)
        {
            _resetEvent.Reset();
            return _resetEvent.WaitOne(timeout);
        }

        public string GetName() => "Query References";
    }
}
