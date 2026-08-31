using RevitMCPCommandSet.Models.Architecture;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services.Architecture
{
    public class CreateRoofEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication _uiApp;
        private UIDocument _uiDoc => _uiApp.ActiveUIDocument;
        private Document _doc => _uiDoc.Document;

        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public List<RoofInfo> RoofData { get; private set; }

        public AIResult<List<int>> Result { get; private set; }

        private List<string> _warnings = new List<string>();

        public void SetParameters(List<RoofInfo> data)
        {
            RoofData = data;
            _resetEvent.Reset();
        }

        public void Execute(UIApplication uiapp)
        {
            _uiApp = uiapp;

            try
            {
                var elementIds = new List<int>();
                _warnings.Clear();

                foreach (var info in RoofData)
                {
                    Level level = FindNearestLevel(info.Level / 304.8);
                    if (level == null) continue;

                    RoofType roofType = null;
                    if (info.Options != null && info.Options.TryGetValue("typeId", out object typeIdObj) && typeIdObj is long typeIdLong)
                    {
                        roofType = _doc.GetElement(new ElementId((int)typeIdLong)) as RoofType;
                    }

                    if (roofType == null && !string.IsNullOrEmpty(info.Type))
                    {
                        roofType = new FilteredElementCollector(_doc)
                            .OfClass(typeof(RoofType))
                            .Cast<RoofType>()
                            .FirstOrDefault(rt => rt.Name.Equals(info.Type, StringComparison.OrdinalIgnoreCase));
                        if (roofType == null)
                        {
                            _warnings.Add($"Roof type '{info.Type}' not found, using first available");
                        }
                    }

                    if (roofType == null)
                    {
                        roofType = new FilteredElementCollector(_doc)
                            .OfClass(typeof(RoofType))
                            .Cast<RoofType>()
                            .FirstOrDefault();
                    }

                    if (roofType == null) continue;

                    using (Transaction tx = new Transaction(_doc, "Create Roof"))
                    {
                        tx.Start();

                        try
                        {
                            // Create footprint roof using CurveArray
                            double elevationInFeet = info.Level / 304.8;
                            double widthInFeet = (info.Options != null && info.Options.TryGetValue("width", out object w)) ? Convert.ToDouble(w) / 304.8 : 30.0 / 304.8;
                            double lengthInFeet = (info.Options != null && info.Options.TryGetValue("length", out object l)) ? Convert.ToDouble(l) / 304.8 : 30.0 / 304.8;

                            CurveArray curveArray = new CurveArray();
                            var p1 = new XYZ(0, 0, elevationInFeet);
                            var p2 = new XYZ(widthInFeet, 0, elevationInFeet);
                            var p3 = new XYZ(widthInFeet, lengthInFeet, elevationInFeet);
                            var p4 = new XYZ(0, lengthInFeet, elevationInFeet);

                            curveArray.Append(Line.CreateBound(p1, p2));
                            curveArray.Append(Line.CreateBound(p2, p3));
                            curveArray.Append(Line.CreateBound(p3, p4));
                            curveArray.Append(Line.CreateBound(p4, p1));

                            ModelCurveArray modelCurveArray = new ModelCurveArray();
                            FootPrintRoof roof = _doc.Create.NewFootPrintRoof(curveArray, level, roofType, out modelCurveArray);

                            if (roof != null)
                            {
                                // Set slope for each curve if overhang specified
                                if (info.Slope > 0)
                                {
                                    double slopeInFeet = info.Slope / 304.8;
                                    foreach (ModelCurve mc in modelCurveArray)
                                    {
                                        roof.set_DefinesSlope(mc, false);
                                    }
                                }

                                elementIds.Add(roof.Id.GetIntValue());
                            }

                            tx.Commit();
                        }
                        catch (Exception ex)
                        {
                            tx.RollBack();
                            _warnings.Add($"Failed to create roof: {ex.Message}");
                        }
                    }
                }

                string message = $"Successfully created {elementIds.Count} roof(s)";
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
                    Message = $"Error creating roofs: {ex.Message}",
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
            return "Create Roof";
        }
    }
}
