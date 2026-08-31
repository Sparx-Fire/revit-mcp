using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests.Views;

public class SetCategoryOverridesTests : RevitApiTest
{
    private static Document _doc;
    private static Level _level;
    private static ViewPlan _floorPlan;

    [Before(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Setup()
    {
        _doc = Application.NewProjectDocument(UnitSystem.Imperial);
        using var tx = new Transaction(_doc, "Setup");
        tx.Start();
        _level = Level.Create(_doc, 0.0);
        _level.Name = "Test Level";
        var floorPlanType = new FilteredElementCollector(_doc)
            .OfClass(typeof(ViewFamilyType))
            .Cast<ViewFamilyType>()
            .FirstOrDefault(vft => vft.ViewFamily == ViewFamily.FloorPlan);
        _floorPlan = ViewPlan.Create(_doc, floorPlanType.Id, _level.Id);
        tx.Commit();
    }

    [After(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Cleanup() => _doc?.Close(false);

    [Test]
    public async Task SetCategoryOverrides_LineColor_OverrideApplied()
    {
        using var tx = new Transaction(_doc, "Set Overrides");
        tx.Start();
        var wallCategory = Category.GetCategory(_doc, BuiltInCategory.OST_Walls);
        var overrides = new OverrideGraphicSettings();
        overrides.SetProjectionLineColor(new Color(255, 0, 0));
        _floorPlan.SetCategoryOverrides(wallCategory.Id, overrides);
        tx.Commit();
        var result = _floorPlan.GetCategoryOverrides(wallCategory.Id);
        await Assert.That((int)result.ProjectionLineColor.Red).IsEqualTo(255);
    }

    [Test]
    public async Task SetCategoryOverrides_FillPattern_OverrideApplied()
    {
        using var tx = new Transaction(_doc, "Set Fill Override");
        tx.Start();
        var wallCategory = Category.GetCategory(_doc, BuiltInCategory.OST_Walls);
        var overrides = new OverrideGraphicSettings();
        overrides.SetSurfaceForegroundPatternColor(new Color(0, 0, 255));
        _floorPlan.SetCategoryOverrides(wallCategory.Id, overrides);
        tx.Commit();
        var result = _floorPlan.GetCategoryOverrides(wallCategory.Id);
        await Assert.That((int)result.SurfaceForegroundPatternColor.Blue).IsEqualTo(255);
    }
}
