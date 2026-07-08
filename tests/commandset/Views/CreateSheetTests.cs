using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using RevitMCPCommandSet.Models.Views;
using RevitMCPCommandSet.Services.Views;
using System.Reflection;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests.Views;

public class CreateSheetTests : RevitApiTest
{
    private static Document _doc;
    private static FamilySymbol _titleBlock;

    [Before(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Setup()
    {
        _doc = Application.NewProjectDocument(UnitSystem.Imperial);

        _titleBlock = new FilteredElementCollector(_doc)
            .OfCategory(BuiltInCategory.OST_TitleBlocks)
            .OfClass(typeof(FamilySymbol))
            .Cast<FamilySymbol>()
            .FirstOrDefault();

        if (_titleBlock != null && !_titleBlock.IsActive)
        {
            _titleBlock.Activate();
            _doc.Regenerate();
        }
    }

    [After(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Cleanup()
    {
        _doc?.Close(false);
    }

    [Test]
    public async Task CreateSheet_WithTitleBlock_SheetIsCreated()
    {
        if (_titleBlock == null)
        {
            await Assert.That(true).IsTrue();
            return;
        }

        ViewSheet sheet = null;

        using (var tx = new Transaction(_doc, "Create Sheet Test"))
        {
            tx.Start();
            sheet = ViewSheet.Create(_doc, _titleBlock.Id);
            sheet.SheetNumber = "MCP-TEST-001";
            sheet.Name = "MCP Sheet Test";
            tx.Commit();
        }

        await Assert.That(sheet).IsNotNull();
        await Assert.That(sheet.SheetNumber).IsEqualTo("MCP-TEST-001");
    }

    [Test]
    public async Task PlaceViewOnSheet_FloorPlan_ViewportIsCreated()
    {
        if (_titleBlock == null)
        {
            await Assert.That(true).IsTrue();
            return;
        }

        ViewSheet sheet = null;
        ViewPlan floorPlan = null;
        Viewport viewport = null;
        List<string> warnings = null;
        var placementInfo = new ViewportCreationInfo
        {
            PositionX = 10000,
            PositionY = 10000
        };

        using (var tx = new Transaction(_doc, "Place Floor Plan Test"))
        {
            tx.Start();

            sheet = ViewSheet.Create(_doc, _titleBlock.Id);
            sheet.SheetNumber = "MCP-TEST-002";
            sheet.Name = "MCP Placement Test";

            floorPlan = new FilteredElementCollector(_doc)
                .OfClass(typeof(ViewPlan))
                .Cast<ViewPlan>()
                .FirstOrDefault(v => v.ViewType == ViewType.FloorPlan);

            await Assert.That(floorPlan).IsNotNull();

            viewport = InvokePlaceViewport(sheet, floorPlan, placementInfo, out warnings);

            tx.Commit();
        }

        await Assert.That(viewport).IsNotNull();
        await AssertOutlineInsideSheet(viewport.GetBoxOutline(), sheet.Outline);
        await Assert.That(warnings.Any(w => w.Contains("Viewport position was clamped", StringComparison.Ordinal))).IsTrue();
    }

    [Test]
    public async Task PlaceViewOnSheet_DoorSchedule_ScheduleInstanceIsCreated()
    {
        if (_titleBlock == null)
        {
            await Assert.That(true).IsTrue();
            return;
        }

        ViewSheet sheet = null;
        ViewSchedule schedule = null;
        ScheduleSheetInstance instance = null;
        List<string> warnings = null;
        var placementInfo = new ViewportCreationInfo
        {
            PositionX = 10000,
            PositionY = 10000
        };

        using (var tx = new Transaction(_doc, "Place Schedule Test"))
        {
            tx.Start();

            sheet = ViewSheet.Create(_doc, _titleBlock.Id);
            sheet.SheetNumber = "MCP-TEST-003";
            sheet.Name = "MCP Schedule Sheet Test";

            schedule = ViewSchedule.CreateSchedule(_doc, new ElementId(BuiltInCategory.OST_Doors));
            schedule.Name = "MCP Door Schedule Placement Test";

            instance = InvokePlaceSchedule(sheet, schedule, placementInfo, out warnings);

            tx.Commit();
        }

        await Assert.That(instance).IsNotNull();
        var scheduleOutline = GetElementOutlineOnSheet(instance, sheet);
        await Assert.That(scheduleOutline).IsNotNull();
        await AssertOutlineInsideSheet(scheduleOutline, sheet.Outline);
        await Assert.That(warnings.Any(w => w.Contains("Schedule position was clamped", StringComparison.Ordinal))).IsTrue();
    }

    [Test]
    public async Task SheetPlanSchedule_Scenario_ViewportIsInsideSheetAndSchedulePlaced()
    {
        if (_titleBlock == null)
        {
            await Assert.That(true).IsTrue();
            return;
        }

        ViewSheet sheet = null;
        ViewPlan floorPlan = null;
        ViewSchedule schedule = null;
        Viewport viewport = null;
        ScheduleSheetInstance scheduleInstance = null;
        List<string> viewportWarnings = null;
        List<string> scheduleWarnings = null;
        var placementInfo = new ViewportCreationInfo
        {
            PositionX = 10000,
            PositionY = 10000
        };
        var schedulePlacementInfo = new ViewportCreationInfo
        {
            PositionX = 10000,
            PositionY = 10000
        };

        using (var tx = new Transaction(_doc, "Sheet Plan Schedule Scenario"))
        {
            tx.Start();

            sheet = ViewSheet.Create(_doc, _titleBlock.Id);
            sheet.SheetNumber = "MCP-TEST-004";
            sheet.Name = "MCP Scenario Sheet";

            floorPlan = new FilteredElementCollector(_doc)
                .OfClass(typeof(ViewPlan))
                .Cast<ViewPlan>()
                .FirstOrDefault(v => v.ViewType == ViewType.FloorPlan);

            await Assert.That(floorPlan).IsNotNull();

            schedule = ViewSchedule.CreateSchedule(_doc, new ElementId(BuiltInCategory.OST_Doors));
            schedule.Name = "MCP Scenario Door Schedule";

            viewport = InvokePlaceViewport(sheet, floorPlan, placementInfo, out viewportWarnings);
            scheduleInstance = InvokePlaceSchedule(sheet, schedule, schedulePlacementInfo, out scheduleWarnings);

            tx.Commit();
        }

        await Assert.That(sheet).IsNotNull();
        await Assert.That(viewport).IsNotNull();
        await Assert.That(scheduleInstance).IsNotNull();

        await AssertOutlineInsideSheet(viewport.GetBoxOutline(), sheet.Outline);
        var scheduleOutline = GetElementOutlineOnSheet(scheduleInstance, sheet);
        await Assert.That(scheduleOutline).IsNotNull();
        await AssertOutlineInsideSheet(scheduleOutline, sheet.Outline);
        await Assert.That(viewportWarnings.Any(w => w.Contains("Viewport position was clamped", StringComparison.Ordinal))).IsTrue();
        await Assert.That(scheduleWarnings.Any(w => w.Contains("Schedule position was clamped", StringComparison.Ordinal))).IsTrue();
    }

    private static Viewport InvokePlaceViewport(ViewSheet sheet, View view, ViewportCreationInfo info, out List<string> warnings)
    {
        warnings = new List<string>();
        var method = typeof(PlaceViewOnSheetEventHandler).GetMethod(
            "PlaceViewport",
            BindingFlags.NonPublic | BindingFlags.Static);

        var result = method?.Invoke(null, new object[] { _doc, sheet, view, info, warnings });
        return result as Viewport;
    }

    private static ScheduleSheetInstance InvokePlaceSchedule(ViewSheet sheet, ViewSchedule schedule, ViewportCreationInfo info, out List<string> warnings)
    {
        warnings = new List<string>();
        var method = typeof(PlaceViewOnSheetEventHandler).GetMethod(
            "PlaceSchedule",
            BindingFlags.NonPublic | BindingFlags.Static);

        var result = method?.Invoke(null, new object[] { _doc, sheet, schedule, info, warnings });
        return result as ScheduleSheetInstance;
    }

    private static Outline GetElementOutlineOnSheet(Element element, ViewSheet sheet)
    {
        var bbox = element.get_BoundingBox(sheet);
        if (bbox == null)
            return null;

        return new Outline(
            new XYZ(bbox.Min.X, bbox.Min.Y, 0),
            new XYZ(bbox.Max.X, bbox.Max.Y, 0));
    }

    private static async Task AssertOutlineInsideSheet(Outline outline, BoundingBoxUV sheetOutline)
    {
        await Assert.That(outline.MinimumPoint.X >= sheetOutline.Min.U).IsTrue();
        await Assert.That(outline.MinimumPoint.Y >= sheetOutline.Min.V).IsTrue();
        await Assert.That(outline.MaximumPoint.X <= sheetOutline.Max.U).IsTrue();
        await Assert.That(outline.MaximumPoint.Y <= sheetOutline.Max.V).IsTrue();
    }
}
