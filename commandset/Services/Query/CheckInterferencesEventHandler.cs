using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services.Query
{
    public class CheckInterferencesEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private Document Doc => uiApp.ActiveUIDocument.Document;
        private readonly ManualResetEvent _resetEvent = new(false);
        public int[] ElementIds { get; private set; }
        public AIResult<object> Result { get; private set; }

        public void SetParameters(int[] elementIds)
        {
            ElementIds = elementIds;
            _resetEvent.Reset();
        }

        public void Execute(UIApplication app)
        {
            uiApp = app;
            try
            {
                if (ElementIds == null || ElementIds.Length < 2)
                {
                    Result = new AIResult<object> { Success = false, Message = "At least two element IDs required for interference check" };
                    return;
                }
                var elementIds = ElementIds.Select(id => new ElementId(id)).ToList();
                var collisions = new List<object>();
                var options = new Options { ComputeReferences = true, DetailLevel = ViewDetailLevel.Fine };

                for (int i = 0; i < elementIds.Count; i++)
                {
                    var elem1 = Doc.GetElement(elementIds[i]);
                    if (elem1 == null) continue;
                    var geom1 = elem1.get_Geometry(options);
                    Solid solid1 = GetFirstSolid(geom1);
                    if (solid1 == null) continue;

                    for (int j = i + 1; j < elementIds.Count; j++)
                    {
                        var elem2 = Doc.GetElement(elementIds[j]);
                        if (elem2 == null) continue;
                        var geom2 = elem2.get_Geometry(options);
                        Solid solid2 = GetFirstSolid(geom2);
                        if (solid2 == null) continue;

                        bool overlaps = false;
                        string intersectionType = "Unknown";
#if REVIT2025_OR_GREATER
#if REVIT2026_OR_GREATER
                        // R26: Solid.Intersect removed
#else
                        IntersectionResultArray intersectionResult = null;
                        SetComparisonResult result = solid1.Intersect(solid2, out intersectionResult);
                        overlaps = result == SetComparisonResult.Overlap || result == SetComparisonResult.Subset || result == SetComparisonResult.Superset;
                        intersectionType = result.ToString();
#endif
#else
                        // R20-R21: Solid.Intersect not available, skip
#endif
                        if (overlaps)
                        {
                            collisions.Add(new
                            {
                                ElementId1 = elementIds[i].GetIntValue(),
                                ElementId2 = elementIds[j].GetIntValue(),
                                IntersectionType = intersectionType,
                                Element1Name = elem1.Name,
                                Element2Name = elem2.Name,
                                Element1Category = elem1.Category?.Name,
                                Element2Category = elem2.Category?.Name
                            });
                        }
                    }
                }

                Result = new AIResult<object>
                {
                    Success = true,
                    Response = new
                    {
                        TotalPairsChecked = (elementIds.Count * (elementIds.Count - 1)) / 2,
                        CollisionCount = collisions.Count,
                        Collisions = collisions
                    }
                };
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

        private Solid GetFirstSolid(GeometryElement geomElement)
        {
            if (geomElement == null) return null;
            foreach (var geomObj in geomElement)
            {
                if (geomObj is Solid solid && solid.Faces.Size > 0)
                    return solid;
                if (geomObj is GeometryInstance instance)
                {
                    var result = GetFirstSolid(instance.GetInstanceGeometry());
                    if (result != null) return result;
                }
            }
            return null;
        }

        public bool WaitForCompletion(int timeout = 30000)
        {
            _resetEvent.Reset();
            return _resetEvent.WaitOne(timeout);
        }

        public string GetName() => "Check Interferences";
    }
}
