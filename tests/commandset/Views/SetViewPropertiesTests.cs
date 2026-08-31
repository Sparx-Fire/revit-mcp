using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests.Views;

public class SetViewPropertiesTests : RevitApiTest
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
    public async Task SetViewProperties_DetailLevel_PropertyChanged()
    {
        using var tx = new Transaction(_doc, "Set Detail Level");
        tx.Start();
        _floorPlan.DetailLevel = ViewDetailLevel.Fine;
        tx.Commit();
        await Assert.That(_floorPlan.DetailLevel).IsEqualTo(ViewDetailLevel.Fine);
    }

    [Test]
    public async Task SetViewProperties_Scale_PropertyChanged()
    {
        using var tx = new Transaction(_doc, "Set Scale");
        tx.Start();
        _floorPlan.Scale = 50;
        tx.Commit();
        await Assert.That(_floorPlan.Scale).IsEqualTo(50);
    }

    [Test]
    public async Task SetViewProperties_DisplayStyle_PropertyChanged()
    {
        using var tx = new Transaction(_doc, "Set Display Style");
        tx.Start();
        _floorPlan.DisplayStyle = DisplayStyle.Wireframe;
        tx.Commit();
        await Assert.That(_floorPlan.DisplayStyle).IsEqualTo(DisplayStyle.Wireframe);
    }
}
