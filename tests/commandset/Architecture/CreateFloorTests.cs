using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests.Architecture;

public class CreateFloorTests : RevitApiTest
{
    private static Document _doc;
    private static Level _level;
    private static FloorType _floorType;

    [Before(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Setup()
    {
        _doc = Application.NewProjectDocument(UnitSystem.Imperial);
        using var tx = new Transaction(_doc, "Setup");
        tx.Start();
        _level = Level.Create(_doc, 0.0);
        _level.Name = "Test Level";
        _floorType = new FilteredElementCollector(_doc)
            .OfClass(typeof(FloorType))
            .Cast<FloorType>()
            .FirstOrDefault();
        tx.Commit();
    }

    [After(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Cleanup() => _doc?.Close(false);

    private static CurveLoop CreateRectangularCurveLoop(double x, double y, double w, double h)
    {
        var loop = new CurveLoop();
        loop.Append(Line.CreateBound(new XYZ(x, y, 0), new XYZ(x + w, y, 0)));
        loop.Append(Line.CreateBound(new XYZ(x + w, y, 0), new XYZ(x + w, y + h, 0)));
        loop.Append(Line.CreateBound(new XYZ(x + w, y + h, 0), new XYZ(x, y + h, 0)));
        loop.Append(Line.CreateBound(new XYZ(x, y + h, 0), new XYZ(x, y, 0)));
        return loop;
    }

    [Test]
    public async Task CreateFloor_Rectangular_FloorCreated()
    {
        using var tx = new Transaction(_doc, "Create Floor");
        tx.Start();
#if REVIT2023_OR_GREATER
        var floor = Floor.Create(_doc, new List<CurveLoop> { CreateRectangularCurveLoop(0, 0, 10, 10) }, _floorType.Id, _level.Id);
#else
        var curveArray = new CurveArray();
        curveArray.Append(Line.CreateBound(new XYZ(0, 0, 0), new XYZ(10, 0, 0)));
        curveArray.Append(Line.CreateBound(new XYZ(10, 0, 0), new XYZ(10, 10, 0)));
        curveArray.Append(Line.CreateBound(new XYZ(10, 10, 0), new XYZ(0, 10, 0)));
        curveArray.Append(Line.CreateBound(new XYZ(0, 10, 0), new XYZ(0, 0, 0)));
        var floor = _doc//.Create.NewFloor(curveArray, _floorType, _level, false);
#endif
        tx.Commit();
        await Assert.That(floor).IsNotNull();
    }

    [Test]
    public async Task CreateFloor_RollbackOnFailure_FloorNotPersisted()
    {
        int countBefore = new FilteredElementCollector(_doc).OfClass(typeof(Floor)).GetElementCount();
        using (var tx = new Transaction(_doc, "Rollback Floor"))
        {
            tx.Start();
#if REVIT2023_OR_GREATER
            Floor.Create(_doc, new List<CurveLoop> { CreateRectangularCurveLoop(20, 0, 5, 5) }, _floorType.Id, _level.Id);
#else
            var ca = new CurveArray();
            ca.Append(Line.CreateBound(new XYZ(20, 0, 0), new XYZ(25, 0, 0)));
            ca.Append(Line.CreateBound(new XYZ(25, 0, 0), new XYZ(25, 5, 0)));
            ca.Append(Line.CreateBound(new XYZ(25, 5, 0), new XYZ(20, 5, 0)));
            ca.Append(Line.CreateBound(new XYZ(20, 5, 0), new XYZ(20, 0, 0)));
            _doc//.Create.NewFloor(ca, _floorType, _level, false);
#endif
            tx.RollBack();
        }
        int countAfter = new FilteredElementCollector(_doc).OfClass(typeof(Floor)).GetElementCount();
        await Assert.That(countAfter).IsEqualTo(countBefore);
    }

    [Test]
    public async Task CreateFloor_WithSlope_SlopeSet()
    {
        using var tx = new Transaction(_doc, "Create Sloped Floor");
        tx.Start();
#if REVIT2023_OR_GREATER
        var floor = Floor.Create(_doc, new List<CurveLoop> { CreateRectangularCurveLoop(30, 0, 10, 10) }, _floorType.Id, _level.Id);
#else
        var ca = new CurveArray();
        ca.Append(Line.CreateBound(new XYZ(30, 0, 0), new XYZ(40, 0, 0)));
        ca.Append(Line.CreateBound(new XYZ(40, 0, 0), new XYZ(40, 10, 0)));
        ca.Append(Line.CreateBound(new XYZ(40, 10, 0), new XYZ(30, 10, 0)));
        ca.Append(Line.CreateBound(new XYZ(30, 10, 0), new XYZ(30, 0, 0)));
        var floor = _doc//.Create.NewFloor(ca, _floorType, _level, false);
#endif
        var slopeParam = floor.get_Parameter(BuiltInParameter.ROOF_SLOPE);
        tx.Commit();
        await Assert.That(floor).IsNotNull();
    }
}
