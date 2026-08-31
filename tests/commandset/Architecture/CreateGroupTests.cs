using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests.Architecture;

public class CreateGroupTests : RevitApiTest
{
    private static Document _doc;
    private static Level _level;
    private static Wall _wall1;
    private static Wall _wall2;

    [Before(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Setup()
    {
        _doc = Application.NewProjectDocument(UnitSystem.Imperial);
        using var tx = new Transaction(_doc, "Setup");
        tx.Start();
        _level = Level.Create(_doc, 0.0);
        _level.Name = "Test Level";
        _wall1 = Wall.Create(_doc, Line.CreateBound(new XYZ(0, 0, 0), new XYZ(5, 0, 0)), _level.Id, false);
        _wall2 = Wall.Create(_doc, Line.CreateBound(new XYZ(5, 0, 0), new XYZ(10, 0, 0)), _level.Id, false);
        tx.Commit();
    }

    [After(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Cleanup() => _doc?.Close(false);

    [Test]
    public async Task CreateGroup_FromWalls_GroupCreated()
    {
        using var tx = new Transaction(_doc, "Create Group");
        tx.Start();
        var ids = new List<ElementId> { _wall1.Id, _wall2.Id };
        var group = _doc.Create.NewGroup(ids);
        tx.Commit();
        await Assert.That(group).IsNotNull();
    }

    [Test]
    public async Task CreateGroup_Ungroup_ElementsRemain()
    {
        using var tx = new Transaction(_doc, "Group and Ungroup");
        tx.Start();
        var ids = new List<ElementId> { _wall1.Id, _wall2.Id };
        var group = _doc.Create.NewGroup(ids);
        group.UngroupMembers();
        tx.Commit();
        var wall1 = _doc.GetElement(_wall1.Id);
        var wall2 = _doc.GetElement(_wall2.Id);
        await Assert.That(wall1).IsNotNull();
        await Assert.That(wall2).IsNotNull();
    }

    [Test]
    public async Task CreateGroup_RollbackOnFailure_GroupNotPersisted()
    {
        int countBefore = new FilteredElementCollector(_doc).OfClass(typeof(Group)).GetElementCount();
        using (var tx = new Transaction(_doc, "Rollback Group"))
        {
            tx.Start();
            var ids = new List<ElementId> { _wall1.Id };
            _doc.Create.NewGroup(ids);
            tx.RollBack();
        }
        int countAfter = new FilteredElementCollector(_doc).OfClass(typeof(Group)).GetElementCount();
        await Assert.That(countAfter).IsEqualTo(countBefore);
    }
}
