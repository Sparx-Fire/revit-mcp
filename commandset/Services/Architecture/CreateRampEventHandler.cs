using Autodesk.Revit.DB.Architecture;
using RevitMCPCommandSet.Models.Architecture;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services.Architecture
{
    public class CreateRampEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication _uiApp;
        private UIDocument _uiDoc => _uiApp.ActiveUIDocument;
        private Document _doc => _uiDoc.Document;

        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public List<RampCreationInfo> RampData { get; private set; }

        public AIResult<List<int>> Result { get; private set; }

        private List<string> _warnings = new List<string>();

        public void SetParameters(List<RampCreationInfo> data)
        {
            RampData = data;
            _resetEvent.Reset();
        }

        public void Execute(UIApplication uiapp)
        {
            _uiApp = uiapp;

            try
            {
                var elementIds = new List<int>();
                _warnings.Clear();

                foreach (var info in RampData)
                {
                    Level baseLevel = FindNearestLevel(info.BaseLevel / 304.8);
                    Level topLevel = FindNearestLevel(info.TopLevel / 304.8);
                    if (baseLevel == null || topLevel == null) continue;

                    using (Transaction tx = new Transaction(_doc, "Create Ramp"))
                    {
                        tx.Start();

                        try
                        {
#if REVIT2026_OR_GREATER
                            // R26: Ramp API changed - use doc.Create.NewRamp
                            _warnings.Add("Ramp creation not yet supported in Revit 2026");
                            tx.RollBack();
                            continue;
#elif REVIT2025_OR_GREATER
                            RampType rampType = null;
                            if (info.TypeId > 0)
                            {
                                rampType = _doc.GetElement(new ElementId(info.TypeId)) as RampType;
                            }

                            if (rampType == null && !string.IsNullOrEmpty(info.RampType))
                            {
                                rampType = new FilteredElementCollector(_doc)
                                    .OfClass(typeof(RampType))
                                    .Cast<RampType>()
                                    .FirstOrDefault(rt => rt.Name.Equals(info.RampType, StringComparison.OrdinalIgnoreCase));
                                if (rampType == null)
                                {
                                    _warnings.Add($"Ramp type '{info.RampType}' not found, using first available");
                                }
                            }

                            if (rampType == null)
                            {
                                rampType = new FilteredElementCollector(_doc)
                                    .OfClass(typeof(RampType))
                                    .Cast<RampType>()
                                    .FirstOrDefault();
                            }

                            if (rampType == null) { tx.RollBack(); continue; }

                            // Build ramp runs
                            IList<RampRun> runs = new List<RampRun>();
                            double widthInFeet = info.Width / 304.8;

                            if (info.PathPoints != null && info.PathPoints.Count >= 2)
                            {
                                for (int i = 0; i < info.PathPoints.Count - 1; i++)
                                {
                                    XYZ startPt = JZPoint.ToXYZ(info.PathPoints[i]);
                                    XYZ endPt = JZPoint.ToXYZ(info.PathPoints[i + 1]);
                                    Line runLine = Line.CreateBound(startPt, endPt);

                                    RampRun run = RampRun.Create(_doc, rampType.Id, baseLevel.Id, topLevel.Id, runLine, RampRunJustification.Center);
                                    runs.Add(run);
                                }
                            }
                            else if (info.StartPoint != null && info.EndPoint != null)
                            {
                                XYZ startPt = JZPoint.ToXYZ(info.StartPoint);
                                XYZ endPt = JZPoint.ToXYZ(info.EndPoint);
                                Line runLine = Line.CreateBound(startPt, endPt);

                                RampRun run = RampRun.Create(_doc, rampType.Id, baseLevel.Id, topLevel.Id, runLine, RampRunJustification.Center);
                                runs.Add(run);
                            }

                            if (runs.Count == 0) { tx.RollBack(); continue; }

                            Ramp ramp = Ramp.Create(_doc, rampType.Id, baseLevel.Id, topLevel.Id, runs);
                            if (ramp != null)
                            {
                                elementIds.Add(ramp.Id.GetIntValue());
                            }
#else
                            _warnings.Add("Ramp creation requires Revit 2022 or later");
                            tx.RollBack();
                            continue;
#endif

                            tx.Commit();
                        }
                        catch (Exception ex)
                        {
                            tx.RollBack();
                            _warnings.Add($"Failed to create ramp: {ex.Message}");
                        }
                    }
                }

                string message = $"Successfully created {elementIds.Count} ramp(s)";
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
                    Message = $"Error creating ramps: {ex.Message}",
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
            return "Create Ramp";
        }
    }
}
