using RevitMCPCommandSet.Models.Architecture;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services.Architecture
{
    public class CreateCeilingEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication _uiApp;
        private UIDocument _uiDoc => _uiApp.ActiveUIDocument;
        private Document _doc => _uiDoc.Document;

        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public List<CeilingCreationInfo> CeilingData { get; private set; }

        public AIResult<List<int>> Result { get; private set; }

        private List<string> _warnings = new List<string>();

        public void SetParameters(List<CeilingCreationInfo> data)
        {
            CeilingData = data;
            _resetEvent.Reset();
        }

        public void Execute(UIApplication uiapp)
        {
            _uiApp = uiapp;

            try
            {
                var elementIds = new List<int>();
                _warnings.Clear();

                foreach (var info in CeilingData)
                {
                    Level level = FindNearestLevel(info.Level / 304.8);
                    if (level == null) continue;

                    CeilingType ceilingType = null;
                    if (info.TypeId > 0)
                    {
                        ceilingType = _doc.GetElement(new ElementId(info.TypeId)) as CeilingType;
                    }

                    if (ceilingType == null && !string.IsNullOrEmpty(info.CeilingType))
                    {
                        ceilingType = new FilteredElementCollector(_doc)
                            .OfClass(typeof(CeilingType))
                            .Cast<CeilingType>()
                            .FirstOrDefault(ct => ct.Name.Equals(info.CeilingType, StringComparison.OrdinalIgnoreCase));
                        if (ceilingType == null)
                        {
                            _warnings.Add($"Ceiling type '{info.CeilingType}' not found, using first available");
                        }
                    }

                    if (ceilingType == null)
                    {
                        ceilingType = new FilteredElementCollector(_doc)
                            .OfClass(typeof(CeilingType))
                            .Cast<CeilingType>()
                            .FirstOrDefault();
                    }

                    if (ceilingType == null) continue;

                    using (Transaction tx = new Transaction(_doc, "Create Ceiling"))
                    {
                        tx.Start();

                        try
                        {
                            CurveLoop curveLoop = new CurveLoop();
                            var points = info.BoundaryPoints;
                            if (points.Count < 3) continue;

                            double elevationInFeet = (info.Level + info.LevelOffset) / 304.8;
                            for (int i = 0; i < points.Count; i++)
                            {
                                var p0 = points[i];
                                var p1 = points[(i + 1) % points.Count];
                                XYZ start = new XYZ(p0.X / 304.8, p0.Y / 304.8, elevationInFeet);
                                XYZ end = new XYZ(p1.X / 304.8, p1.Y / 304.8, elevationInFeet);
                                curveLoop.Append(Line.CreateBound(start, end));
                            }

                            IList<CurveLoop> curveLoops = new List<CurveLoop> { curveLoop };

                            Ceiling ceiling = VersionCompat.CreateCeiling(_doc, curveLoops, ceilingType.Id, level.Id);

                            if (ceiling != null)
                            {
                                elementIds.Add(ceiling.Id.GetIntValue());
                            }

                            tx.Commit();
                        }
                        catch (Exception ex)
                        {
                            tx.RollBack();
                            _warnings.Add($"Failed to create ceiling: {ex.Message}");
                        }
                    }
                }

                string message = $"Successfully created {elementIds.Count} ceiling(s)";
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
                    Message = $"Error creating ceilings: {ex.Message}",
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
            return "Create Ceiling";
        }
    }
}
