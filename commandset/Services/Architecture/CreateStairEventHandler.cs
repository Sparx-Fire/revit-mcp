using Autodesk.Revit.DB.Architecture;
using RevitMCPCommandSet.Models.Architecture;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services.Architecture
{
    public class CreateStairEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication _uiApp;
        private UIDocument _uiDoc => _uiApp.ActiveUIDocument;
        private Document _doc => _uiDoc.Document;

        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public List<StairCreationInfo> StairData { get; private set; }

        public AIResult<List<int>> Result { get; private set; }

        private List<string> _warnings = new List<string>();

        public void SetParameters(List<StairCreationInfo> data)
        {
            StairData = data;
            _resetEvent.Reset();
        }

        public void Execute(UIApplication uiapp)
        {
            _uiApp = uiapp;

            try
            {
                var elementIds = new List<int>();
                _warnings.Clear();

                foreach (var info in StairData)
                {
                    Level baseLevel = FindNearestLevel(info.BaseLevel / 304.8);
                    Level topLevel = FindNearestLevel(info.TopLevel / 304.8);
                    if (baseLevel == null || topLevel == null) continue;

                    using (Transaction tx = new Transaction(_doc, "Create Stair"))
                    {
                        tx.Start();

                        try
                        {
#if REVIT2026_OR_GREATER
                            // R26: Stairs API changed significantly
                            _warnings.Add("Stair creation not yet supported in Revit 2026");
                            tx.RollBack();
                            continue;
#elif REVIT2025_OR_GREATER
                            StairsType stairsType = null;
                            if (info.TypeId > 0)
                            {
                                stairsType = _doc.GetElement(new ElementId(info.TypeId)) as StairsType;
                            }

                            if (stairsType == null && !string.IsNullOrEmpty(info.StairType))
                            {
                                stairsType = new FilteredElementCollector(_doc)
                                    .OfClass(typeof(StairsType))
                                    .Cast<StairsType>()
                                    .FirstOrDefault(st => st.Name.Equals(info.StairType, StringComparison.OrdinalIgnoreCase));
                                if (stairsType == null)
                                {
                                    _warnings.Add($"Stair type '{info.StairType}' not found, using first available");
                                }
                            }

                            if (stairsType == null)
                            {
                                stairsType = new FilteredElementCollector(_doc)
                                    .OfClass(typeof(StairsType))
                                    .Cast<StairsType>()
                                    .FirstOrDefault();
                            }

                            if (stairsType == null) { tx.RollBack(); continue; }

                            // Build stair runs
                            IList<StairsRun> runs = new List<StairsRun>();
                            IList<StairsLanding> landings = new List<StairsLanding>();

                            double widthInFeet = info.Width / 304.8;

                            // Create stair runs from path points
                            if (info.PathPoints != null && info.PathPoints.Count >= 2)
                            {
                                for (int i = 0; i < info.PathPoints.Count - 1; i++)
                                {
                                    XYZ startPt = JZPoint.ToXYZ(info.PathPoints[i]);
                                    XYZ endPt = JZPoint.ToXYZ(info.PathPoints[i + 1]);
                                    Line runLine = Line.CreateBound(startPt, endPt);

                                    StairsRun run = StairsRun.Create(_doc, stairsType.Id, baseLevel.Id, topLevel.Id, runLine, StairsRunJustification.Center);
                                    runs.Add(run);
                                }
                            }
                            else if (info.StartPoint != null && info.EndPoint != null)
                            {
                                XYZ startPt = JZPoint.ToXYZ(info.StartPoint);
                                XYZ endPt = JZPoint.ToXYZ(info.EndPoint);
                                Line runLine = Line.CreateBound(startPt, endPt);

                                StairsRun run = StairsRun.Create(_doc, stairsType.Id, baseLevel.Id, topLevel.Id, runLine, StairsRunJustification.Center);
                                runs.Add(run);
                            }

                            if (runs.Count == 0) { tx.RollBack(); continue; }

                            // Create landing if needed
                            if (info.HasLanding && runs.Count > 1)
                            {
                                CurveLoop landingLoop = new CurveLoop();
                                double lw = info.LandingWidth > 0 ? info.LandingWidth / 304.8 : widthInFeet;
                                double ld = info.LandingDepth > 0 ? info.LandingDepth / 304.8 : widthInFeet;
                                XYZ lOrigin = JZPoint.ToXYZ(info.PathPoints != null && info.PathPoints.Count > 1
                                    ? info.PathPoints[1] : info.StartPoint);
                                landingLoop.Append(Line.CreateBound(lOrigin, new XYZ(lOrigin.X + lw, lOrigin.Y, lOrigin.Z)));
                                landingLoop.Append(Line.CreateBound(new XYZ(lOrigin.X + lw, lOrigin.Y, lOrigin.Z), new XYZ(lOrigin.X + lw, lOrigin.Y + ld, lOrigin.Z)));
                                landingLoop.Append(Line.CreateBound(new XYZ(lOrigin.X + lw, lOrigin.Y + ld, lOrigin.Z), new XYZ(lOrigin.X, lOrigin.Y + ld, lOrigin.Z)));
                                landingLoop.Append(Line.CreateBound(new XYZ(lOrigin.X, lOrigin.Y + ld, lOrigin.Z), lOrigin));

                                StairsLanding landing = StairsLanding.Create(_doc, landingLoop, baseLevel.Id);
                                landings.Add(landing);
                            }

                            Stairs stair = Stairs.Create(_doc, stairsType.Id, baseLevel.Id, topLevel.Id, runs, landings);
                            if (stair != null)
                            {
                                elementIds.Add(stair.Id.GetIntValue());
                            }
#else
                            _warnings.Add("Stair creation requires Revit 2022 or later");
                            tx.RollBack();
                            continue;
#endif

                            tx.Commit();
                        }
                        catch (Exception ex)
                        {
                            tx.RollBack();
                            _warnings.Add($"Failed to create stair: {ex.Message}");
                        }
                    }
                }

                string message = $"Successfully created {elementIds.Count} stair(s)";
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
                    Message = $"Error creating stairs: {ex.Message}",
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
            return "Create Stair";
        }
    }
}
