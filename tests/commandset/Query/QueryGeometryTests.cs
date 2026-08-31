using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests.Query;

public class QueryGeometryTests : RevitApiTest
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
    public async Task QueryGeometry_GetGeometry_GeometryFound()
    {
        var options = new Options { DetailLevel = ViewDetailLevel.Fine, ComputeReferences = true };
        var geom = _wall.get_Geometry(options);
        await Assert.That(geom).IsNotNull();
    }

    [Test]
    public async Task QueryGeometry_GetSolid_SolidFound()
    {
        var options = new Options { DetailLevel = ViewDetailLevel.Fine };
        var geom = _wall.get_Geometry(options);
        Solid solid = null;
        if (geom != null)
        {
            foreach (var obj in geom)
            {
                if (obj is Solid s && s.Faces.Size > 0)
                {
                    solid = s;
                    break;
                }
            }
        }
        await Assert.That(solid).IsNotNull();
    }

    [Test]
    public async Task QueryGeometry_GetBoundingBox_BoundingBoxFound()
    {
        var bb = _wall.get_BoundingBox(null);
        await Assert.That(bb).IsNotNull();
        await Assert.That(bb.Max.X).IsGreaterThan(bb.Min.X);
    }
}
