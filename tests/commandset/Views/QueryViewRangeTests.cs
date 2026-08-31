using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests.Views;

public class QueryViewRangeTests : RevitApiTest
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
    public async Task QueryViewRange_GetViewRange_RangeNotNull()
    {
#if REVIT2023_OR_GREATER
        var viewRange = _floorPlan.GetViewRange();
        await Assert.That(viewRange).IsNotNull();
#endif
    }

    [Test]
    public async Task QueryViewRange_GetLevelId_LevelIdReturned()
    {
#if REVIT2023_OR_GREATER
        var viewRange = _floorPlan.GetViewRange();
        var levelId = viewRange.GetLevelId(PlanViewPlane.TopClipPlane);
        await Assert.That(levelId).IsNotEqualTo(ElementId.InvalidElementId);
#endif
    }
}
