using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPCommandSet.Models.DataExtraction;
using RevitMCPCommandSet.Services;
using RevitMCPCommandSet.Utils;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services.DataExtraction
{
    public class ValidateScheduleEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private static readonly Dictionary<string, BuiltInCategory> CategoryMap =
            new Dictionary<string, BuiltInCategory>(StringComparer.OrdinalIgnoreCase)
            {
                { "Doors", BuiltInCategory.OST_Doors },
                { "Windows", BuiltInCategory.OST_Windows },
                { "Floors", BuiltInCategory.OST_Floors },
                { "OST_Doors", BuiltInCategory.OST_Doors },
                { "OST_Windows", BuiltInCategory.OST_Windows },
                { "OST_Floors", BuiltInCategory.OST_Floors },
            };

        private string _category;
        private string _scheduleName;
        private string _levelName;
        private long? _levelId;

        public ValidateScheduleResult ResultInfo { get; private set; }
        public bool TaskCompleted { get; private set; }
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public void SetParameters(string category, string scheduleName = null, string levelName = null, long? levelId = null)
        {
            _category = category;
            _scheduleName = scheduleName;
            _levelName = levelName;
            _levelId = levelId;
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

                if (string.IsNullOrWhiteSpace(_category))
                {
                    throw new ArgumentException("Category is required. Supported values: Doors, Windows, Floors.");
                }

                if (!CategoryMap.TryGetValue(_category.Trim(), out var builtInCategory))
                {
                    throw new ArgumentException(
                        $"Unsupported category '{_category}'. Supported values: Doors, Windows, Floors.");
                }

                var targetLevel = ResolveLevel(doc);
                var schedule = FindSchedule(doc, builtInCategory, _scheduleName);
                if (schedule == null)
                {
                    var scheduleHint = string.IsNullOrWhiteSpace(_scheduleName)
                        ? $"No schedule found for category {_category}."
                        : $"No schedule named '{_scheduleName}' found for category {_category}.";
                    throw new InvalidOperationException(scheduleHint);
                }

                var modelIds = CollectModelElementIds(doc, builtInCategory, targetLevel);
                var scheduleIds = CollectScheduleElementIds(doc, schedule, targetLevel);

                var missingIds = modelIds
                    .Except(scheduleIds)
                    .Select(GetElementIdValue)
                    .OrderBy(id => id)
                    .ToList();

                var modelCount = modelIds.Count;
                var scheduleCount = scheduleIds.Count;

                ResultInfo = new ValidateScheduleResult
                {
                    Category = _category,
                    ScheduleName = schedule.Name,
                    LevelName = targetLevel?.Name,
                    ModelCount = modelCount,
                    ScheduleCount = scheduleCount,
                    Diff = modelCount - scheduleCount,
                    MissingIds = missingIds,
                    Success = true,
                    Message = missingIds.Count == 0
                        ? $"Schedule '{schedule.Name}' matches the model for {_category} ({modelCount} elements)."
                        : $"Schedule '{schedule.Name}' is missing {missingIds.Count} of {modelCount} model elements for {_category}."
                };
            }
            catch (Exception ex)
            {
                ResultInfo = new ValidateScheduleResult
                {
                    Category = _category,
                    ScheduleName = _scheduleName,
                    LevelName = _levelName,
                    Success = false,
                    Message = $"Error validating schedule: {ex.Message}"
                };
            }
            finally
            {
                TaskCompleted = true;
                _resetEvent.Set();
            }
        }

        public string GetName()
        {
            return "Validate Schedule";
        }

        private Level ResolveLevel(Document doc)
        {
            if (_levelId.HasValue)
            {
#if REVIT2024_OR_GREATER
                var levelById = doc.GetElement(new ElementId(_levelId.Value)) as Level;
#else
                var levelById = doc.GetElement(new ElementId((int)_levelId.Value)) as Level;
#endif
                if (levelById == null)
                {
                    throw new ArgumentException($"Level with id {_levelId.Value} was not found.");
                }

                return levelById;
            }

            if (!string.IsNullOrWhiteSpace(_levelName))
            {
                var levelByName = new FilteredElementCollector(doc)
                    .OfClass(typeof(Level))
                    .Cast<Level>()
                    .FirstOrDefault(level => level.Name.Equals(_levelName.Trim(), StringComparison.OrdinalIgnoreCase));

                if (levelByName == null)
                {
                    throw new ArgumentException($"Level '{_levelName}' was not found.");
                }

                return levelByName;
            }

            return null;
        }

        private static ViewSchedule FindSchedule(Document doc, BuiltInCategory builtInCategory, string scheduleName)
        {
            var schedules = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewSchedule))
                .Cast<ViewSchedule>()
                .Where(schedule => !schedule.IsTemplate)
                .ToList();

            if (!string.IsNullOrWhiteSpace(scheduleName))
            {
                return schedules.FirstOrDefault(schedule =>
                    schedule.Name.Equals(scheduleName.Trim(), StringComparison.OrdinalIgnoreCase));
            }

            var category = Category.GetCategory(doc, builtInCategory);
            return schedules.FirstOrDefault(schedule => schedule.Definition.CategoryId == category.Id);
        }

        private HashSet<ElementId> CollectModelElementIds(Document doc, BuiltInCategory builtInCategory, Level targetLevel)
        {
            var elements = new FilteredElementCollector(doc)
                .OfCategory(builtInCategory)
                .WhereElementIsNotElementType()
                .ToElements();

            return FilterElementsByLevel(doc, elements, targetLevel);
        }

        private HashSet<ElementId> CollectScheduleElementIds(Document doc, ViewSchedule schedule, Level targetLevel)
        {
            var elements = new FilteredElementCollector(doc, schedule.Id)
                .WhereElementIsNotElementType()
                .ToElements();

            return FilterElementsByLevel(doc, elements, targetLevel);
        }

        private static HashSet<ElementId> FilterElementsByLevel(Document doc, ICollection<Element> elements, Level targetLevel)
        {
            if (targetLevel == null)
            {
                return elements.Select(element => element.Id).ToHashSet();
            }

            var targetLevelId = targetLevel.Id.GetIntValue();
            return elements
                .Where(element =>
                {
                    var levelInfo = AIElementFilterEventHandler.GetElementLevel(doc, element);
                    return levelInfo != null && levelInfo.Id == targetLevelId;
                })
                .Select(element => element.Id)
                .ToHashSet();
        }

        private static long GetElementIdValue(ElementId elementId) => elementId.GetValue();
    }
}
