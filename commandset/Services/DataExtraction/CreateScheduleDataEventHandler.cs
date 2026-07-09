using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPCommandSet.Models.DataExtraction;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services.DataExtraction
{
    public class CreateScheduleDataEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private ScheduleElementCategory _category;

        public ScheduleExportResult ResultInfo { get; private set; } = new ScheduleExportResult();
        public bool TaskCompleted { get; private set; }
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public void SetParameters(ScheduleElementCategory category)
        {
            _category = category;
            TaskCompleted = false;
            _resetEvent.Reset();
        }

        public bool WaitForCompletion(int timeoutMilliseconds = 10000)
        {
            _resetEvent.Reset();
            return _resetEvent.WaitOne(timeoutMilliseconds);
        }

        public void Execute(UIApplication app)
        {
            try
            {
                var doc = app.ActiveUIDocument.Document;
                var instanceRows = _category switch
                {
                    ScheduleElementCategory.Doors => CollectDoorRows(doc),
                    ScheduleElementCategory.Windows => CollectWindowRows(doc),
                    ScheduleElementCategory.Floors => CollectFloorRows(doc),
                    _ => new List<ScheduleInstanceRow>()
                };

                var groups = instanceRows
                    .GroupBy(r => new { r.TypeId, r.Level, r.Size, r.Type, r.FamilyName })
                    .Select(g => new ScheduleGroupRow
                    {
                        TypeId = g.Key.TypeId,
                        FamilyName = g.Key.FamilyName,
                        Type = g.Key.Type,
                        Size = g.Key.Size,
                        Level = g.Key.Level,
                        Count = g.Count(),
                        Mark = BuildGroupMark(g.Select(x => x.Mark))
                    })
                    .OrderBy(g => g.Level)
                    .ThenBy(g => g.FamilyName)
                    .ThenBy(g => g.Type)
                    .ToList();

                ResultInfo = new ScheduleExportResult
                {
                    Category = _category.ToString().ToLowerInvariant(),
                    TotalCount = instanceRows.Count,
                    Groups = groups,
                    Success = true,
                    Message = $"Successfully exported {instanceRows.Count} {_category.ToString().ToLowerInvariant()} in {groups.Count} groups"
                };
            }
            catch (Exception ex)
            {
                ResultInfo = new ScheduleExportResult
                {
                    Category = _category.ToString().ToLowerInvariant(),
                    Success = false,
                    Message = $"Error exporting schedule data: {ex.Message}"
                };
            }
            finally
            {
                TaskCompleted = true;
                _resetEvent.Set();
            }
        }

        public string GetName() => "Create Schedule Data";

        private static List<ScheduleInstanceRow> CollectDoorRows(Document doc) =>
            CollectFamilyInstanceRows(doc, BuiltInCategory.OST_Doors, GetDoorSize);

        private static List<ScheduleInstanceRow> CollectWindowRows(Document doc) =>
            CollectFamilyInstanceRows(doc, BuiltInCategory.OST_Windows, GetWindowSize);

        private static List<ScheduleInstanceRow> CollectFloorRows(Document doc)
        {
            var rows = new List<ScheduleInstanceRow>();
            var floors = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Floors)
                .WhereElementIsNotElementType()
                .Cast<Floor>();

            foreach (var floor in floors)
            {
                var floorType = doc.GetElement(floor.GetTypeId()) as FloorType;
                var level = doc.GetElement(floor.LevelId) as Level;
                rows.Add(new ScheduleInstanceRow
                {
                    Mark = GetParameterString(floor, BuiltInParameter.ALL_MODEL_MARK),
                    FamilyName = floorType?.FamilyName ?? "",
                    Type = floorType?.Name ?? "",
                    Size = FormatFloorSize(floor),
                    Level = level?.Name ?? "No Level",
                    TypeId = GetElementIdValue(floor.GetTypeId())
                });
            }

            return rows;
        }

        private static List<ScheduleInstanceRow> CollectFamilyInstanceRows(
            Document doc,
            BuiltInCategory category,
            Func<FamilyInstance, string> sizeResolver)
        {
            var rows = new List<ScheduleInstanceRow>();
            var instances = new FilteredElementCollector(doc)
                .OfCategory(category)
                .WhereElementIsNotElementType()
                .Cast<FamilyInstance>();

            foreach (var instance in instances)
            {
                var symbol = instance.Symbol;
                rows.Add(new ScheduleInstanceRow
                {
                    Mark = GetParameterString(instance, BuiltInParameter.ALL_MODEL_MARK),
                    FamilyName = symbol?.FamilyName ?? "",
                    Type = symbol?.Name ?? "",
                    Size = sizeResolver(instance),
                    Level = GetInstanceLevelName(doc, instance),
                    TypeId = GetElementIdValue(symbol?.Id ?? ElementId.InvalidElementId)
                });
            }

            return rows;
        }

        private static string GetDoorSize(FamilyInstance instance)
        {
            double width = GetParameterDouble(instance, BuiltInParameter.DOOR_WIDTH);
            double height = GetParameterDouble(instance, BuiltInParameter.DOOR_HEIGHT);
            return FormatWidthHeightMm(width, height, instance.Symbol?.Name);
        }

        private static string GetWindowSize(FamilyInstance instance)
        {
            double width = GetParameterDouble(instance, BuiltInParameter.WINDOW_WIDTH);
            double height = GetParameterDouble(instance, BuiltInParameter.WINDOW_HEIGHT);
            if (width <= 0) width = GetParameterDouble(instance, BuiltInParameter.FAMILY_WIDTH_PARAM);
            if (height <= 0) height = GetParameterDouble(instance, BuiltInParameter.FAMILY_HEIGHT_PARAM);
            return FormatWidthHeightMm(width, height, instance.Symbol?.Name);
        }

        private static string FormatWidthHeightMm(double widthFeet, double heightFeet, string fallback)
        {
            if (widthFeet > 0 && heightFeet > 0)
            {
                int widthMm = (int)Math.Round(widthFeet * 304.8);
                int heightMm = (int)Math.Round(heightFeet * 304.8);
                return $"{widthMm} x {heightMm} mm";
            }

            return fallback ?? "";
        }

        private static string FormatFloorSize(Floor floor)
        {
            double areaSqFt = floor.get_Parameter(BuiltInParameter.HOST_AREA_COMPUTED)?.AsDouble() ?? 0;
            if (areaSqFt > 0)
            {
                double areaM2 = areaSqFt * 0.09290304;
                return $"{Math.Round(areaM2, 2)} m²";
            }

            return "";
        }

        private static string GetInstanceLevelName(Document doc, FamilyInstance instance)
        {
            var levelId = instance.LevelId;
            if (levelId != null && levelId != ElementId.InvalidElementId)
            {
                return (doc.GetElement(levelId) as Level)?.Name ?? "No Level";
            }

            var host = instance.Host;
            if (host != null)
            {
                var hostLevelId = host.get_Parameter(BuiltInParameter.FAMILY_LEVEL_PARAM)?.AsElementId();
                if (hostLevelId != null && hostLevelId != ElementId.InvalidElementId)
                {
                    return (doc.GetElement(hostLevelId) as Level)?.Name ?? "No Level";
                }
            }

            return "No Level";
        }

        private static string GetParameterString(Element element, BuiltInParameter parameter)
        {
            return element.get_Parameter(parameter)?.AsString() ?? "";
        }

        private static double GetParameterDouble(Element element, BuiltInParameter parameter)
        {
            var param = element.get_Parameter(parameter);
            return param?.AsDouble() ?? 0;
        }

        private static long GetElementIdValue(ElementId id)
        {
#if REVIT2024_OR_GREATER
            return id.Value;
#else
            return id.IntegerValue;
#endif
        }

        private static string BuildGroupMark(IEnumerable<string> marks)
        {
            var distinct = marks
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .Distinct()
                .OrderBy(m => m)
                .ToList();

            return distinct.Count == 0 ? "" : string.Join(", ", distinct);
        }

        private sealed class ScheduleInstanceRow
        {
            public string Mark { get; set; } = "";
            public string FamilyName { get; set; } = "";
            public string Type { get; set; } = "";
            public string Size { get; set; } = "";
            public string Level { get; set; } = "";
            public long TypeId { get; set; }
        }
    }
}
