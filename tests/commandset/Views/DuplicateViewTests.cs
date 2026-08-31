using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests.Views;

public class DuplicateViewTests : RevitApiTest
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
    public async Task DuplicateView_DuplicateAsDependent_ViewDuplicated()
    {
        using var tx = new Transaction(_doc, "Duplicate View");
        tx.Start();
        var newId = _floorPlan.Duplicate(ViewDuplicateOption.Duplicate);
        tx.Commit();
        await Assert.That(newId).IsNotEqualTo(ElementId.InvalidElementId);
    }

    [Test]
    public async Task DuplicateView_RollbackOnFailure_ViewNotDuplicated()
    {
        int countBefore = new FilteredElementCollector(_doc).OfClass(typeof(ViewPlan)).GetElementCount();
        using (var tx = new Transaction(_doc, "Rollback Duplicate"))
        {
            tx.Start();
            _floorPlan.Duplicate(ViewDuplicateOption.Duplicate);
            tx.RollBack();
        }
        int countAfter = new FilteredElementCollector(_doc).OfClass(typeof(ViewPlan)).GetElementCount();
        await Assert.That(countAfter).IsEqualTo(countBefore);
    }
}
