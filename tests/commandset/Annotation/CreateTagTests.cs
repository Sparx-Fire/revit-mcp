using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests.Annotation;

public class CreateTagTests : RevitApiTest
{
    private static Document _doc;
    private static Level _level;
    private static ViewPlan _floorPlan;
    private static Wall _wall;
    private static IndependentTag _tag;

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
        _wall = Wall.Create(_doc, Line.CreateBound(new XYZ(0, 0, 0), new XYZ(10, 0, 0)), _level.Id, false);
        tx.Commit();
    }

    [After(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Cleanup() => _doc?.Close(false);

    [Test]
    public async Task CreateTag_OnWall_TagCreated()
    {
        using var tx = new Transaction(_doc, "Create Tag");
        tx.Start();
        var tag = IndependentTag.Create(_doc, _floorPlan.Id, new Reference(_wall), false, TagMode.TM_ADDBY_CATEGORY, TagOrientation.Horizontal, new XYZ(5, 2, 0));
        tx.Commit();
        await Assert.That(tag).IsNotNull();
    }

    [Test]
    public async Task CreateTag_RollbackOnFailure_TagNotPersisted()
    {
        int countBefore = new FilteredElementCollector(_doc).OfClass(typeof(IndependentTag)).GetElementCount();
        using (var tx = new Transaction(_doc, "Rollback Tag"))
        {
            tx.Start();
            IndependentTag.Create(_doc, _floorPlan.Id, new Reference(_wall), false, TagMode.TM_ADDBY_CATEGORY, TagOrientation.Horizontal, new XYZ(5, -2, 0));
            tx.RollBack();
        }
        int countAfter = new FilteredElementCollector(_doc).OfClass(typeof(IndependentTag)).GetElementCount();
        await Assert.That(countAfter).IsEqualTo(countBefore);
    }
}
