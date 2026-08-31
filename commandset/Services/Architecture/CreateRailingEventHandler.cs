using Autodesk.Revit.DB.Architecture;
using RevitMCPCommandSet.Models.Architecture;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services.Architecture
{
    public class CreateRailingEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication _uiApp;
        private UIDocument _uiDoc => _uiApp.ActiveUIDocument;
        private Document _doc => _uiDoc.Document;

        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public List<RailingCreationInfo> RailingData { get; private set; }

        public AIResult<List<int>> Result { get; private set; }

        private List<string> _warnings = new List<string>();

        public void SetParameters(List<RailingCreationInfo> data)
        {
            RailingData = data;
            _resetEvent.Reset();
        }

        public void Execute(UIApplication uiapp)
        {
            _uiApp = uiapp;

            try
            {
                var elementIds = new List<int>();
                _warnings.Clear();

                foreach (var info in RailingData)
                {
                    Level level = FindNearestLevel(info.Level / 304.8);
                    if (level == null) continue;

                    RailingType railingType = null;
                    if (info.TypeId > 0)
                    {
                        railingType = _doc.GetElement(new ElementId(info.TypeId)) as RailingType;
                    }

                    if (railingType == null && !string.IsNullOrEmpty(info.RailingType))
                    {
                        railingType = new FilteredElementCollector(_doc)
                            .OfClass(typeof(RailingType))
                            .Cast<RailingType>()
                            .FirstOrDefault(rt => rt.Name.Equals(info.RailingType, StringComparison.OrdinalIgnoreCase));
                        if (railingType == null)
                        {
                            _warnings.Add($"Railing type '{info.RailingType}' not found, using first available");
                        }
                    }

                    if (railingType == null)
                    {
                        railingType = new FilteredElementCollector(_doc)
                            .OfClass(typeof(RailingType))
                            .Cast<RailingType>()
                            .FirstOrDefault();
                    }

                    if (railingType == null) continue;

                    using (Transaction tx = new Transaction(_doc, "Create Railing"))
                    {
                        tx.Start();

                        try
                        {
                            // Build railing path line
                            Line pathLine = null;
                            if (info.StartPoint != null && info.EndPoint != null)
                            {
                                XYZ start = JZPoint.ToXYZ(info.StartPoint);
                                XYZ end = JZPoint.ToXYZ(info.EndPoint);
                                pathLine = Line.CreateBound(start, end);
                            }
                            else if (info.PathPoints != null && info.PathPoints.Count >= 2)
                            {
                                XYZ start = JZPoint.ToXYZ(info.PathPoints[0]);
                                XYZ end = JZPoint.ToXYZ(info.PathPoints[info.PathPoints.Count - 1]);
                                pathLine = Line.CreateBound(start, end);
                            }

                            if (pathLine == null) continue;

#if REVIT2026_OR_GREATER
                            // R26: Railing.Create takes CurveLoop
                            CurveLoop curveLoop = new CurveLoop();
                            curveLoop.Append(pathLine);
                            Railing railing = Railing.Create(_doc, curveLoop, railingType.Id, level.Id);
#elif REVIT2025_OR_GREATER
                            Railing railing = Railing.Create(_doc, pathLine, railingType.Id, level.Id);
#else
                            // R20 uses CurveLoop-based Railing.Create
                            CurveLoop curveLoop = new CurveLoop();
                            curveLoop.Append(pathLine);
                            Railing railing = Railing.Create(_doc, curveLoop, railingType.Id, level.Id);
#endif

                            if (railing != null)
                            {
                                // Set railing height if specified
#if REVIT2026_OR_GREATER
                                if (info.Height > 0 && info.Height != 1070)
                                {
                                    // R26: RAILING_HEIGHT removed, set via parameter lookup
                                    Parameter heightParam = railing.LookupParameter("Height");
                                    if (heightParam != null && !heightParam.IsReadOnly)
                                    {
                                        heightParam.Set(info.Height / 304.8);
                                    }
                                }
#elif REVIT2025_OR_GREATER
                                if (info.Height > 0 && info.Height != 1070)
                                {
                                    Parameter heightParam = railing.get_Parameter(BuiltInParameter.RAILING_HEIGHT);
                                    if (heightParam != null && !heightParam.IsReadOnly)
                                    {
                                        heightParam.Set(info.Height / 304.8);
                                    }
                                }
#endif

                                elementIds.Add(railing.Id.GetIntValue());
                            }

                            tx.Commit();
                        }
                        catch (Exception ex)
                        {
                            tx.RollBack();
                            _warnings.Add($"Failed to create railing: {ex.Message}");
                        }
                    }
                }

                string message = $"Successfully created {elementIds.Count} railing(s)";
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
                    Message = $"Error creating railings: {ex.Message}",
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
            return "Create Railing";
        }
    }
}
