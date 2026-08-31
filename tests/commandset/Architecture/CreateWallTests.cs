using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests.Architecture;

public class CreateWallTests : RevitApiTest
{
    private static Document _doc;
    private static Level _level;
    private static WallType _wallType;

    [Before(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Setup()
    {
        _doc = Application.NewProjectDocument(UnitSystem.Imperial);
        using var tx = new Transaction(_doc, "Setup");
        tx.Start();
        _level = Level.Create(_doc, 0.0);
        _level.Name = "Test Level";
        _wallType = new FilteredElementCollector(_doc)
            .OfClass(typeof(WallType))
            .Cast<WallType>()
            .FirstOrDefault();
        tx.Commit();
    }

    [After(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Cleanup() => _doc?.Close(false);

    [Test]
    public async Task CreateWall_LinearWall_WallCreated()
    {
        using var tx = new Transaction(_doc, "Create Wall");
        tx.Start();
        var wall = Wall.Create(_doc, Line.CreateBound(new XYZ(0, 0, 0), new XYZ(10, 0, 0)), _level.Id, false);
        tx.Commit();
        await Assert.That(wall).IsNotNull();
        await Assert.That(wall.Location is LocationCurve).IsTrue();
    }

    [Test]
    public async Task CreateWall_SetHeight_WallHeightMatches()
    {
        using var tx = new Transaction(_doc, "Create Wall With Height");
        tx.Start();
        var wall = Wall.Create(_doc, Line.CreateBound(new XYZ(0, 0, 0), new XYZ(10, 0, 0)), _level.Id, false);
        var param = wall.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM);
        param?.Set(10.0);
        tx.Commit();
        await Assert.That(wall.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM)?.AsDouble()).IsEqualTo(10.0);
    }

    [Test]
    public async Task CreateWall_CurvedWall_ArcWallCreated()
    {
        using var tx = new Transaction(_doc, "Create Curved Wall");
        tx.Start();
        var arc = Arc.Create(new XYZ(0, 0, 0), new XYZ(10, 0, 0), new XYZ(5, 5, 0));
        var wall = Wall.Create(_doc, arc, _level.Id, false);
        tx.Commit();
        await Assert.That(wall).IsNotNull();
    }

    [Test]
    public async Task CreateWall_RollbackOnFailure_WallNotPersisted()
    {
        int countBefore = new FilteredElementCollector(_doc).OfClass(typeof(Wall)).GetElementCount();
        using (var tx = new Transaction(_doc, "Rollback Wall"))
        {
            tx.Start();
            Wall.Create(_doc, Line.CreateBound(new XYZ(0, 0, 0), new XYZ(5, 0, 0)), _level.Id, false);
            tx.RollBack();
        }
        int countAfter = new FilteredElementCollector(_doc).OfClass(typeof(Wall)).GetElementCount();
        await Assert.That(countAfter).IsEqualTo(countBefore);
    }

    [Test]
    public async Task CreateWall_WithWallType_SpecificTypeApplied()
    {
        using var tx = new Transaction(_doc, "Create Wall With Type");
        tx.Start();
        var wall = Wall.Create(_doc, Line.CreateBound(new XYZ(0, 0, 0), new XYZ(10, 0, 0)), _wallType.Id, _level.Id, 10.0, 0.0, false, false);
        tx.Commit();
        await Assert.That(wall.WallType.Id).IsEqualTo(_wallType.Id);
    }
}
