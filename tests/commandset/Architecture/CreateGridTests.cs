using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests.Architecture;

public class CreateGridTests : RevitApiTest
{
    private static Document _doc;

    [Before(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Setup()
    {
        _doc = Application.NewProjectDocument(UnitSystem.Imperial);
    }

    [After(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Cleanup() => _doc?.Close(false);

    [Test]
    public async Task CreateGrid_Line_GridCreated()
    {
        using var tx = new Transaction(_doc, "Create Grid");
        tx.Start();
        var grid = Grid.Create(_doc, Line.CreateBound(new XYZ(0, 0, 0), new XYZ(0, 10, 0)));
        tx.Commit();
        await Assert.That(grid).IsNotNull();
    }

    [Test]
    public async Task CreateGrid_SetName_NameApplied()
    {
        using var tx = new Transaction(_doc, "Create Named Grid");
        tx.Start();
        var grid = Grid.Create(_doc, Line.CreateBound(new XYZ(10, 0, 0), new XYZ(10, 10, 0)));
        grid.Name = "A";
        tx.Commit();
        await Assert.That(grid.Name).IsEqualTo("A");
    }

    [Test]
    public async Task CreateGrid_RollbackOnFailure_GridNotPersisted()
    {
        int countBefore = new FilteredElementCollector(_doc).OfClass(typeof(Grid)).GetElementCount();
        using (var tx = new Transaction(_doc, "Rollback Grid"))
        {
            tx.Start();
            Grid.Create(_doc, Line.CreateBound(new XYZ(20, 0, 0), new XYZ(20, 10, 0)));
            tx.RollBack();
        }
        int countAfter = new FilteredElementCollector(_doc).OfClass(typeof(Grid)).GetElementCount();
        await Assert.That(countAfter).IsEqualTo(countBefore);
    }
}
