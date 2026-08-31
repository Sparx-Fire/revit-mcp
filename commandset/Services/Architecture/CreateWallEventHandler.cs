using RevitMCPCommandSet.Models.Architecture;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services.Architecture
{
    public class CreateWallEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication _uiApp;
        private UIDocument _uiDoc => _uiApp.ActiveUIDocument;
        private Document _doc => _uiDoc.Document;

        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public List<WallCreationInfo> WallData { get; private set; }

        public AIResult<List<int>> Result { get; private set; }

        private List<string> _warnings = new List<string>();

        public void SetParameters(List<WallCreationInfo> data)
        {
            WallData = data;
            _resetEvent.Reset();
        }

        public void Execute(UIApplication uiapp)
        {
            _uiApp = uiapp;

            try
            {
                var elementIds = new List<int>();
                _warnings.Clear();

                foreach (var info in WallData)
                {
                    // Find nearest level from base level elevation
                    Level baseLevel = FindNearestLevel(info.BaseLevel / 304.8);
                    if (baseLevel == null) continue;

                    double baseOffset = (info.BaseOffset + info.BaseLevel) / 304.8 - baseLevel.Elevation;

                    // Get wall type
                    WallType wallType = null;
                    if (info.TypeId > 0)
                    {
                        wallType = _doc.GetElement(new ElementId(info.TypeId)) as WallType;
                    }

                    if (wallType == null && !string.IsNullOrEmpty(info.WallType))
                    {
                        wallType = new FilteredElementCollector(_doc)
                            .OfClass(typeof(WallType))
                            .Cast<WallType>()
                            .FirstOrDefault(wt => wt.Name.Equals(info.WallType, StringComparison.OrdinalIgnoreCase));
                        if (wallType == null)
                        {
                            _warnings.Add($"Wall type '{info.WallType}' not found, using first available");
                        }
                    }

                    if (wallType == null)
                    {
                        wallType = new FilteredElementCollector(_doc)
                            .OfClass(typeof(WallType))
                            .Cast<WallType>()
                            .FirstOrDefault();
                    }

                    if (wallType == null) continue;

                    using (Transaction tx = new Transaction(_doc, "Create Wall"))
                    {
                        tx.Start();

                        try
                        {
                            XYZ start = JZPoint.ToXYZ(info.StartPoint);
                            XYZ end = JZPoint.ToXYZ(info.EndPoint);
                            Line curve = Line.CreateBound(start, end);

                            double height = info.Height / 304.8;

                            Wall wall = Wall.Create(_doc, curve, wallType.Id, baseLevel.Id, height, baseOffset, info.Flipped, info.IsStructural);

                            if (wall != null)
                            {
                                elementIds.Add(wall.Id.GetIntValue());
                            }

                            tx.Commit();
                        }
                        catch (Exception ex)
                        {
                            tx.RollBack();
                            _warnings.Add($"Failed to create wall: {ex.Message}");
                        }
                    }
                }

                string message = $"Successfully created {elementIds.Count} wall(s)";
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
                    Message = $"Error creating walls: {ex.Message}",
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
            return "Create Wall";
        }
    }
}
