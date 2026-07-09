using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using RevitMCPCommandSet.Models.Architecture;
using RevitMCPCommandSet.Services;
using RevitMCPCommandSet.Utils;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests.Architecture;

public class ConfigureGridDisplayTests : RevitApiTest
{
    private static Document _doc;
    private static ViewPlan _level1Plan;
    private static ViewPlan _level2Plan;
    private static Grid _verticalGrid;
    private static Grid _horizontalGrid;

    [Before(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Setup()
    {
        _doc = Application.NewProjectDocument(UnitSystem.Imperial);

        using var tx = new Transaction(_doc, "Setup Grid Display Test");
        tx.Start();

        var level1 = Level.Create(_doc, 0.0);
        level1.Name = "Grid Level 1";
        var level2 = Level.Create(_doc, 10.0);
        level2.Name = "Grid Level 2";

        var floorPlanType = new FilteredElementCollector(_doc)
            .OfClass(typeof(ViewFamilyType))
            .Cast<ViewFamilyType>()
            .First(vft => vft.ViewFamily == ViewFamily.FloorPlan);

        _level1Plan = ViewPlan.Create(_doc, floorPlanType.Id, level1.Id);
        _level2Plan = ViewPlan.Create(_doc, floorPlanType.Id, level2.Id);

        _verticalGrid = Grid.Create(
            _doc,
            Line.CreateBound(new XYZ(10, 0, 0), new XYZ(10, 30, 0)));
        _verticalGrid.Name = "A";

        _horizontalGrid = Grid.Create(
            _doc,
            Line.CreateBound(new XYZ(0, 15, 0), new XYZ(20, 15, 0)));
        _horizontalGrid.Name = "1";

        tx.Commit();
    }

    [After(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Cleanup()
    {
        _doc?.Close(false);
    }

    [Test]
    public async Task ConfigureGridDisplay_AllFloorPlans_UpdatesGridExtents()
    {
        GridDisplayConfigurationResult result;

        using (var tx = new Transaction(_doc, "Configure Grid Display"))
        {
            tx.Start();
            result = GridDisplayHelper.ConfigureGrids(
                _doc,
                new[] { _verticalGrid, _horizontalGrid },
                new GridDisplayConfigurationInfo
                {
                    XExtentMin = 0,
                    XExtentMax = 20000,
                    YExtentMin = 0,
                    YExtentMax = 30000,
                    ShowBubbles = true,
                    ApplyToAllFloorPlans = true
                });
            tx.Commit();
        }

        await Assert.That(result.GridsProcessed).IsEqualTo(2);
        await Assert.That(result.ViewsProcessed).IsGreaterThanOrEqualTo(2);
        await Assert.That(result.GridViewUpdates).IsGreaterThanOrEqualTo(4);

        var verticalCurves = _verticalGrid.GetCurvesInView(
            DatumExtentType.ViewSpecific,
            _level1Plan);
        var verticalCurveLevel2List = _verticalGrid.GetCurvesInView(
            DatumExtentType.ViewSpecific,
            _level2Plan);

        var verticalCurveLevel1 = verticalCurves.Count > 0 ? verticalCurves[0] as Line : null;
        var verticalCurveLevel2 = verticalCurveLevel2List.Count > 0 ? verticalCurveLevel2List[0] as Line : null;

        await Assert.That(verticalCurveLevel1).IsNotNull();
        await Assert.That(verticalCurveLevel2).IsNotNull();
        await Assert.That(verticalCurveLevel1.Length).IsGreaterThan(0);
        await Assert.That(verticalCurveLevel2.Length).IsEqualTo(verticalCurveLevel1.Length).Within(0.001);
    }

    [Test]
    public async Task ConfigureGridDisplay_SlightlyOffAxisGrid_UpdatesGridExtents()
    {
        Grid slightlyOffAxisGrid;

        using (var tx = new Transaction(_doc, "Create Off-Axis Grid"))
        {
            tx.Start();
            slightlyOffAxisGrid = Grid.Create(
                _doc,
                Line.CreateBound(
                    new XYZ(15 + 1e-9, 0, 0),
                    new XYZ(15 - 1e-9, 30, 0)));
            slightlyOffAxisGrid.Name = "OffAxis";
            tx.Commit();
        }

        GridDisplayConfigurationResult result;

        using (var tx = new Transaction(_doc, "Configure Off-Axis Grid Display"))
        {
            tx.Start();
            result = GridDisplayHelper.ConfigureGrids(
                _doc,
                new[] { slightlyOffAxisGrid },
                new GridDisplayConfigurationInfo
                {
                    XExtentMin = 0,
                    XExtentMax = 20000,
                    YExtentMin = 0,
                    YExtentMax = 30000,
                    ShowBubbles = true,
                    ApplyToAllFloorPlans = true
                });
            tx.Commit();
        }

        await Assert.That(result.GridViewUpdates).IsGreaterThanOrEqualTo(2);

        var curves = slightlyOffAxisGrid.GetCurvesInView(
            DatumExtentType.ViewSpecific,
            _level1Plan);

        await Assert.That(curves.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task ConfigureGridDisplay_NewGridOnThreeLevels_VisibleOnAllFloorPlans()
    {
        var level3 = Level.Create(_doc, 20.0);
        level3.Name = "Grid Level 3";

        var floorPlanType = new FilteredElementCollector(_doc)
            .OfClass(typeof(ViewFamilyType))
            .Cast<ViewFamilyType>()
            .First(vft => vft.ViewFamily == ViewFamily.FloorPlan);

        ViewPlan level3Plan;

        using (var tx = new Transaction(_doc, "Create Level 3 Plan"))
        {
            tx.Start();
            level3Plan = ViewPlan.Create(_doc, floorPlanType.Id, level3.Id);
            tx.Commit();
        }

        Grid newGrid;

        using (var tx = new Transaction(_doc, "Create Grid For Level 3 Test"))
        {
            tx.Start();
            newGrid = Grid.Create(
                _doc,
                Line.CreateBound(new XYZ(25, 0, 0), new XYZ(25, 30, 0)));
            newGrid.Name = "Level3Grid";
            GridDisplayHelper.EnsureGridSpansAllLevels(_doc, newGrid);
            tx.Commit();
        }

        GridDisplayConfigurationResult result;

        using (var tx = new Transaction(_doc, "Configure Level 3 Grid Display"))
        {
            tx.Start();
            result = GridDisplayHelper.ConfigureGrids(
                _doc,
                new[] { newGrid },
                new GridDisplayConfigurationInfo
                {
                    XExtentMin = 0,
                    XExtentMax = 20000,
                    YExtentMin = 0,
                    YExtentMax = 30000,
                    ShowBubbles = true,
                    ApplyToAllFloorPlans = true
                });
            tx.Commit();
        }

        await Assert.That(result.ViewsProcessed).IsGreaterThanOrEqualTo(3);
        await Assert.That(result.GridViewUpdates).IsGreaterThanOrEqualTo(3);
        await Assert.That(newGrid.CanBeVisibleInView(level3Plan)).IsTrue();

        var level3Curves = newGrid.GetCurvesInView(DatumExtentType.ViewSpecific, level3Plan);
        await Assert.That(level3Curves.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task ConfigureGridDisplay_ExistingGrids_AppliesProjectGridType()
    {
        var projectGridType = new FilteredElementCollector(_doc)
            .OfClass(typeof(GridType))
            .Cast<GridType>()
            .FirstOrDefault();

        await Assert.That(projectGridType).IsNotNull();

        using (var tx = new Transaction(_doc, "Apply Grid Type"))
        {
            tx.Start();
            GridDisplayHelper.ConfigureGrids(
                _doc,
                new[] { _verticalGrid },
                new GridDisplayConfigurationInfo
                {
                    GridTypeId = projectGridType!.Id.GetIntValue(),
                    ApplyToAllFloorPlans = false
                });
            tx.Commit();
        }

        await Assert.That(_verticalGrid.GetTypeId()).IsEqualTo(projectGridType!.Id);
    }
}
