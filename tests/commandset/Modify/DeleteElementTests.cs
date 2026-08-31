using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests.Modify;

public class DeleteElementTests : RevitApiTest
{
    private static Document _doc;
    private static Level _level;
    private static Wall _wall;

    [Before(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Setup()
    {
        _doc = Application.NewProjectDocument(UnitSystem.Imperial);
        using var tx = new Transaction(_doc, "Setup");
        tx.Start();
        _level = Level.Create(_doc, 0.0);
        _level.Name = "Test Level";
        _wall = Wall.Create(_doc, Line.CreateBound(new XYZ(0, 0, 0), new XYZ(10, 0, 0)), _level.Id, false);
        tx.Commit();
    }

    [After(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Cleanup() => _doc?.Close(false);

    [Test]
    public async Task DeleteElement_Wall_WallDeleted()
    {
        var wallId = _wall.Id;
        using var tx = new Transaction(_doc, "Delete Wall");
        tx.Start();
        _doc.Delete(wallId);
        tx.Commit();
        var deleted = _doc.GetElement(wallId);
        await Assert.That(deleted).IsNull();
    }

    [Test]
    public async Task DeleteElement_RollbackOnFailure_ElementNotDeleted()
    {
        var wallId = _wall.Id;
        using (var tx = new Transaction(_doc, "Rollback Delete"))
        {
            tx.Start();
            _doc.Delete(wallId);
            tx.RollBack();
        }
        var restored = _doc.GetElement(wallId);
        await Assert.That(restored).IsNotNull();
    }
}
