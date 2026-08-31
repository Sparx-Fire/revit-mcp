using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests.Views;

public class SetViewRangeTests : RevitApiTest
{
    private static Document _doc;
    private static Level _level1;
    private static Level _level2;
    private static ViewPlan _floorPlan;

    [Before(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Setup()
    {
        _doc = Application.NewProjectDocument(UnitSystem.Imperial);
        using var tx = new Transaction(_doc, "Setup");
        tx.Start();
        _level1 = Level.Create(_doc, 0.0);
        _level1.Name = "Level 1";
        _level2 = Level.Create(_doc, 10.0);
        _level2.Name = "Level 2";
        var floorPlanType = new FilteredElementCollector(_doc)
            .OfClass(typeof(ViewFamilyType))
            .Cast<ViewFamilyType>()
            .FirstOrDefault(vft => vft.ViewFamily == ViewFamily.FloorPlan);
        _floorPlan = ViewPlan.Create(_doc, floorPlanType.Id, _level1.Id);
        tx.Commit();
    }

    [After(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Cleanup() => _doc?.Close(false);

    [Test]
    public async Task SetViewRange_TopLevel_ViewRangeUpdated()
    {
        using var tx = new Transaction(_doc, "Set View Range");
        tx.Start();
#if REVIT2023_OR_GREATER
        var viewRange = _floorPlan.GetViewRange();
        viewRange.SetLevelId(PlanViewPlane.TopClipPlane, _level2.Id);
        _floorPlan.SetViewRange(viewRange);
#endif
        tx.Commit();
#if REVIT2023_OR_GREATER
        var updatedRange = _floorPlan.GetViewRange();
        await Assert.That(updatedRange.GetLevelId(PlanViewPlane.TopClipPlane)).IsEqualTo(_level2.Id);
#endif
    }

    [Test]
    public async Task SetViewRange_CutOffset_OffsetApplied()
    {
        using var tx = new Transaction(_doc, "Set Cut Offset");
        tx.Start();
#if REVIT2023_OR_GREATER
        var viewRange = _floorPlan.GetViewRange();
        viewRange.SetOffset(PlanViewPlane.CutPlane, 5.0);
        _floorPlan.SetViewRange(viewRange);
#endif
        tx.Commit();
#if REVIT2023_OR_GREATER
        var updatedRange = _floorPlan.GetViewRange();
        await Assert.That(updatedRange.GetOffset(PlanViewPlane.CutPlane)).IsEqualTo(5.0);
#endif
    }
}
