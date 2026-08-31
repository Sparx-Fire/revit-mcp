using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests.Views;

public class CreateViewTests : RevitApiTest
{
    private static Document _doc;
    private static Level _level;
    private static ViewFamilyType _floorPlanType;

    [Before(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Setup()
    {
        _doc = Application.NewProjectDocument(UnitSystem.Imperial);
        using var tx = new Transaction(_doc, "Setup");
        tx.Start();
        _level = Level.Create(_doc, 0.0);
        _level.Name = "Test Level";
        _floorPlanType = new FilteredElementCollector(_doc)
            .OfClass(typeof(ViewFamilyType))
            .Cast<ViewFamilyType>()
            .FirstOrDefault(vft => vft.ViewFamily == ViewFamily.FloorPlan);
        tx.Commit();
    }

    [After(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Cleanup() => _doc?.Close(false);

    [Test]
    public async Task CreateView_FloorPlan_ViewCreated()
    {
        using var tx = new Transaction(_doc, "Create Floor Plan");
        tx.Start();
        var view = ViewPlan.Create(_doc, _floorPlanType.Id, _level.Id);
        tx.Commit();
        await Assert.That(view).IsNotNull();
        await Assert.That(view.ViewType).IsEqualTo(ViewType.FloorPlan);
    }

    [Test]
    public async Task CreateView_SetName_NameApplied()
    {
        using var tx = new Transaction(_doc, "Create Named View");
        tx.Start();
        var view = ViewPlan.Create(_doc, _floorPlanType.Id, _level.Id);
        view.Name = "My Test Floor Plan";
        tx.Commit();
        await Assert.That(view.Name).IsEqualTo("My Test Floor Plan");
    }

    [Test]
    public async Task CreateView_SetScale_ScaleApplied()
    {
        using var tx = new Transaction(_doc, "Create View With Scale");
        tx.Start();
        var view = ViewPlan.Create(_doc, _floorPlanType.Id, _level.Id);
        view.Scale = 200;
        tx.Commit();
        await Assert.That(view.Scale).IsEqualTo(200);
    }

    [Test]
    public async Task CreateView_RollbackOnFailure_ViewNotPersisted()
    {
        int countBefore = new FilteredElementCollector(_doc).OfClass(typeof(ViewPlan)).GetElementCount();
        using (var tx = new Transaction(_doc, "Rollback View"))
        {
            tx.Start();
            ViewPlan.Create(_doc, _floorPlanType.Id, _level.Id);
            tx.RollBack();
        }
        int countAfter = new FilteredElementCollector(_doc).OfClass(typeof(ViewPlan)).GetElementCount();
        await Assert.That(countAfter).IsEqualTo(countBefore);
    }
}
