using Autodesk.Revit.DB.Structure;
using RevitMCPCommandSet.Models.Architecture;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services.Architecture
{
    public class CreateColumnEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication _uiApp;
        private UIDocument _uiDoc => _uiApp.ActiveUIDocument;
        private Document _doc => _uiDoc.Document;

        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public List<ColumnInfo> ColumnData { get; private set; }

        public AIResult<List<int>> Result { get; private set; }

        private List<string> _warnings = new List<string>();

        public void SetParameters(List<ColumnInfo> data)
        {
            ColumnData = data;
            _resetEvent.Reset();
        }

        public void Execute(UIApplication uiapp)
        {
            _uiApp = uiapp;

            try
            {
                var elementIds = new List<int>();
                _warnings.Clear();

                foreach (var info in ColumnData)
                {
                    Level baseLevel = FindNearestLevel(info.BaseLevel / 304.8);
                    if (baseLevel == null) continue;

                    FamilySymbol symbol = null;
                    if (info.TypeId > 0)
                    {
                        symbol = _doc.GetElement(new ElementId(info.TypeId)) as FamilySymbol;
                    }

                    if (symbol == null && !string.IsNullOrEmpty(info.Type))
                    {
                        symbol = new FilteredElementCollector(_doc)
                            .OfClass(typeof(FamilySymbol))
                            .Cast<FamilySymbol>()
                            .FirstOrDefault(fs => fs.FamilyName != null &&
                                fs.get_Parameter(BuiltInParameter.ALL_MODEL_TYPE_NAME)?.AsString()?.Equals(info.Type, StringComparison.OrdinalIgnoreCase) == true);
                        if (symbol == null)
                        {
                            _warnings.Add($"Column type '{info.Type}' not found, using first available structural column symbol");
                        }
                    }

                    if (symbol == null)
                    {
                        symbol = new FilteredElementCollector(_doc)
                            .OfClass(typeof(FamilySymbol))
                            .Cast<FamilySymbol>()
                            .FirstOrDefault(fs => fs.FamilyName != null &&
                                fs.Category != null && VersionCompat.GetBuiltInCategory(fs.Category) == BuiltInCategory.OST_StructuralColumns);
                    }

                    if (symbol == null) continue;

                    using (Transaction tx = new Transaction(_doc, "Create Column"))
                    {
                        tx.Start();

                        if (!symbol.IsActive)
                        {
                            symbol.Activate();
                        }

                        try
                        {
                            XYZ location = JZPoint.ToXYZ(info.Location);
                            StructuralType structuralType = info.IsStructural ? StructuralType.Column : StructuralType.NonStructural;

                            FamilyInstance column = _doc.Create.NewFamilyInstance(location, symbol, baseLevel, structuralType);

                            if (column != null)
                            {
                                // Set column height if specified
#if REVIT2026_OR_GREATER
                                if (info.Height > 0)
                                {
                                    // R26: COLUMN_HEIGHT removed, set height via Location
                                    double heightFt = info.Height / 304.8;
                                    column.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM)?.Set(heightFt);
                                }
#elif REVIT2025_OR_GREATER
                                if (info.Height > 0)
                                {
                                    Parameter heightParam = column.get_Parameter(BuiltInParameter.COLUMN_HEIGHT);
                                    if (heightParam != null && !heightParam.IsReadOnly)
                                    {
                                        heightParam.Set(info.Height / 304.8);
                                    }
                                }
#endif

                                elementIds.Add(column.Id.GetIntValue());
                            }

                            tx.Commit();
                        }
                        catch (Exception ex)
                        {
                            tx.RollBack();
                            _warnings.Add($"Failed to create column: {ex.Message}");
                        }
                    }
                }

                string message = $"Successfully created {elementIds.Count} column(s)";
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
                    Message = $"Error creating columns: {ex.Message}",
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
            return "Create Column";
        }
    }
}
