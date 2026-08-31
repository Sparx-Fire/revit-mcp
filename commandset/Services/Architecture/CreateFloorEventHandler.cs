using RevitMCPCommandSet.Models.Architecture;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services.Architecture
{
    public class CreateFloorEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication _uiApp;
        private UIDocument _uiDoc => _uiApp.ActiveUIDocument;
        private Document _doc => _uiDoc.Document;

        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public List<FloorInfo> FloorData { get; private set; }

        public AIResult<List<int>> Result { get; private set; }

        private List<string> _warnings = new List<string>();

        public void SetParameters(List<FloorInfo> data)
        {
            FloorData = data;
            _resetEvent.Reset();
        }

        public void Execute(UIApplication uiapp)
        {
            _uiApp = uiapp;

            try
            {
                var elementIds = new List<int>();
                _warnings.Clear();

                foreach (var info in FloorData)
                {
                    Level level = FindNearestLevel(info.Level / 304.8);
                    if (level == null) continue;

                    FloorType floorType = null;
                    if (info.TypeId > 0)
                    {
                        floorType = _doc.GetElement(new ElementId(info.TypeId)) as FloorType;
                    }

                    if (floorType == null && !string.IsNullOrEmpty(info.FloorType))
                    {
                        floorType = new FilteredElementCollector(_doc)
                            .OfClass(typeof(FloorType))
                            .Cast<FloorType>()
                            .FirstOrDefault(ft => ft.Name.Equals(info.FloorType, StringComparison.OrdinalIgnoreCase));
                        if (floorType == null)
                        {
                            _warnings.Add($"Floor type '{info.FloorType}' not found, using first available");
                        }
                    }

                    if (floorType == null)
                    {
                        floorType = new FilteredElementCollector(_doc)
                            .OfClass(typeof(FloorType))
                            .Cast<FloorType>()
                            .FirstOrDefault();
                    }

                    if (floorType == null) continue;

                    using (Transaction tx = new Transaction(_doc, "Create Floor"))
                    {
                        tx.Start();

                        try
                        {
                            // Build boundary curve loop from boundary points
                            CurveLoop curveLoop = new CurveLoop();
                            var points = info.BoundaryPoints;
                            if (points.Count < 3) continue;

                            double elevationInFeet = info.Level / 304.8;
                            for (int i = 0; i < points.Count; i++)
                            {
                                var p0 = points[i];
                                var p1 = points[(i + 1) % points.Count];
                                XYZ start = new XYZ(p0.X / 304.8, p0.Y / 304.8, elevationInFeet);
                                XYZ end = new XYZ(p1.X / 304.8, p1.Y / 304.8, elevationInFeet);
                                curveLoop.Append(Line.CreateBound(start, end));
                            }

                            IList<CurveLoop> curveLoops = new List<CurveLoop> { curveLoop };

                            Floor floor = VersionCompat.CreateFloor(_doc, curveLoops, floorType.Id, level.Id);

                            if (floor != null)
                            {
                                // Set height offset if specified
                                if (info.LevelOffset != 0)
                                {
                                    Parameter offsetParam = floor.get_Parameter(BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM);
                                    if (offsetParam != null && !offsetParam.IsReadOnly)
                                    {
                                        offsetParam.Set(info.LevelOffset / 304.8);
                                    }
                                }

                                elementIds.Add(floor.Id.GetIntValue());
                            }

                            tx.Commit();
                        }
                        catch (Exception ex)
                        {
                            tx.RollBack();
                            _warnings.Add($"Failed to create floor: {ex.Message}");
                        }
                    }
                }

                string message = $"Successfully created {elementIds.Count} floor(s)";
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
                    Message = $"Error creating floors: {ex.Message}",
                };
            }
            finally
            {
                _resetEvent.Set();
            }
        }

        private Level FindNearestLevel(double elevationInFeet)
        {
            var levels = new FilteredElementCollector(_doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .ToList();

            Level nearestLevel = null;
            double minDistance = double.MaxValue;

            foreach (var level in levels)
            {
                double distance = Math.Abs(level.Elevation - elevationInFeet);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestLevel = level;
                }
            }

            return nearestLevel;
        }

        public bool WaitForCompletion(int timeoutMilliseconds = 10000)
        {
            _resetEvent.Reset();
            return _resetEvent.WaitOne(timeoutMilliseconds);
        }

        public string GetName()
        {
            return "Create Floor";
        }
    }
}
