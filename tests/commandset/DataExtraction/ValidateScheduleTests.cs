using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests.DataExtraction;

public class ValidateScheduleTests : RevitApiTest
{
    private static Document _doc;
    private static Level _level;
    private static ViewSchedule _floorSchedule;

    [Before(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Setup()
    {
        _doc = Application.NewProjectDocument(UnitSystem.Imperial);

        using var tx = new Transaction(_doc, "Setup Validate Schedule Test");
        tx.Start();

        _level = Level.Create(_doc, 0.0);
        _level.Name = "Schedule Test Level";

        var floorType = new FilteredElementCollector(_doc)
            .OfClass(typeof(FloorType))
            .Cast<FloorType>()
            .FirstOrDefault();

        if (floorType != null)
        {
            var profile = new List<CurveLoop>();
            var loop = new CurveLoop();
            loop.Append(Line.CreateBound(new XYZ(0, 0, 0), new XYZ(20, 0, 0)));
            loop.Append(Line.CreateBound(new XYZ(20, 0, 0), new XYZ(20, 20, 0)));
            loop.Append(Line.CreateBound(new XYZ(20, 20, 0), new XYZ(0, 20, 0)));
            loop.Append(Line.CreateBound(new XYZ(0, 20, 0), new XYZ(0, 0, 0)));
            profile.Add(loop);

            Floor.Create(_doc, profile, floorType.Id, _level.Id);
        }

        var floorCategory = Category.GetCategory(_doc, BuiltInCategory.OST_Floors);
        _floorSchedule = ViewSchedule.CreateSchedule(_doc, floorCategory.Id);
        _floorSchedule.Name = "Validate Schedule Test Floors";

        tx.Commit();
    }

    [After(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Cleanup()
    {
        _doc?.Close(false);
    }

    [Test]
    public async Task FloorSchedule_MatchesModel_NoMissingIds()
    {
        var modelIds = CollectModelElementIds(BuiltInCategory.OST_Floors, null);
        var scheduleIds = CollectScheduleElementIds(_floorSchedule, null);

        var missingIds = modelIds
            .Except(scheduleIds)
#if REVIT2024_OR_GREATER
            .Select(id => id.Value)
#else
            .Select(id => (long)id.IntegerValue)
#endif
            .OrderBy(id => id)
            .ToList();

        await Assert.That(modelIds.Count).IsGreaterThan(0);
        await Assert.That(scheduleIds.Count).IsEqualTo(modelIds.Count);
        await Assert.That(missingIds).IsEmpty();
        await Assert.That(modelIds.Count - scheduleIds.Count).IsEqualTo(0);
    }

    [Test]
    public async Task FloorSchedule_LevelFilter_ReturnsSubset()
    {
        var modelIds = CollectModelElementIds(BuiltInCategory.OST_Floors, _level);
        var scheduleIds = CollectScheduleElementIds(_floorSchedule, _level);

        await Assert.That(modelIds.Count).IsGreaterThan(0);
        await Assert.That(scheduleIds.Count).IsEqualTo(modelIds.Count);
    }

    [Test]
    public async Task FloorSchedule_WrongLevelFilter_ReturnsEmpty()
    {
        var otherLevel = new FilteredElementCollector(_doc)
            .OfClass(typeof(Level))
            .Cast<Level>()
            .FirstOrDefault(level => level.Id != _level.Id);

        if (otherLevel == null)
        {
            return;
        }

        var modelIds = CollectModelElementIds(BuiltInCategory.OST_Floors, otherLevel);
        await Assert.That(modelIds.Count).IsEqualTo(0);
    }

    [Test]
    public async Task FindSchedule_ByCategory_ReturnsFloorSchedule()
    {
        var schedules = new FilteredElementCollector(_doc)
            .OfClass(typeof(ViewSchedule))
            .Cast<ViewSchedule>()
            .Where(schedule => !schedule.IsTemplate)
            .ToList();

        var floorCategory = Category.GetCategory(_doc, BuiltInCategory.OST_Floors);
        var schedule = schedules.FirstOrDefault(item => item.Definition.CategoryId == floorCategory.Id);

        await Assert.That(schedule).IsNotNull();
        await Assert.That(schedule!.Name).IsEqualTo("Validate Schedule Test Floors");
    }

    private static HashSet<ElementId> CollectModelElementIds(BuiltInCategory builtInCategory, Level targetLevel)
    {
        var elements = new FilteredElementCollector(_doc)
            .OfCategory(builtInCategory)
            .WhereElementIsNotElementType()
            .ToElements();

        return FilterElementsByLevel(elements, targetLevel);
    }

    private static HashSet<ElementId> CollectScheduleElementIds(ViewSchedule schedule, Level targetLevel)
    {
        var elements = new FilteredElementCollector(_doc, schedule.Id)
            .WhereElementIsNotElementType()
            .ToElements();

        return FilterElementsByLevel(elements, targetLevel);
    }

    private static HashSet<ElementId> FilterElementsByLevel(ICollection<Element> elements, Level targetLevel)
    {
        if (targetLevel == null)
        {
            return elements.Select(element => element.Id).ToHashSet();
        }

        var targetLevelId =
#if REVIT2024_OR_GREATER
            targetLevel.Id.Value;
#else
            targetLevel.Id.IntegerValue;
#endif
        return elements
            .Where(element => GetElementLevelId(element) == targetLevelId)
            .Select(element => element.Id)
            .ToHashSet();
    }

    private static long? GetElementLevelId(Element element)
    {
        if (element is Floor floor)
        {
            var levelParam = floor.get_Parameter(BuiltInParameter.LEVEL_PARAM);
            if (levelParam != null && levelParam.HasValue)
            {
#if REVIT2024_OR_GREATER
                return levelParam.AsElementId().Value;
#else
                return levelParam.AsElementId().IntegerValue;
#endif
            }
        }

        return null;
    }
}
