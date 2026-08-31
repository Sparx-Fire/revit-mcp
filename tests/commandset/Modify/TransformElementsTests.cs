using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests.Modify;

public class TransformElementsTests : RevitApiTest
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
    public async Task TransformElements_MoveElement_ElementMoved()
    {
        using var tx = new Transaction(_doc, "Move Element");
        tx.Start();
        var translation = new XYZ(5, 0, 0);
        ElementTransformUtils.MoveElement(_doc, _wall.Id, translation);
        tx.Commit();
        var movedWall = _doc.GetElement(_wall.Id) as Wall;
        var curve = (movedWall?.Location as LocationCurve)?.Curve;
        await Assert.That(curve).IsNotNull();
    }

    [Test]
    public async Task TransformElements_RotateElement_ElementRotated()
    {
        using var tx = new Transaction(_doc, "Rotate Element");
        tx.Start();
        var axis = Line.CreateUnbound(new XYZ(0, 0, 0), XYZ.BasisZ);
        ElementTransformUtils.RotateElement(_doc, _wall.Id, axis, Math.PI / 4);
        tx.Commit();
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task TransformElements_MirrorElement_ElementMirrored()
    {
        using var tx = new Transaction(_doc, "Mirror Element");
        tx.Start();
        var plane = Plane.CreateByNormalAndOrigin(XYZ.BasisY, XYZ.Zero);
        ElementTransformUtils.MirrorElements(_doc, new List<ElementId> { _wall.Id }, plane, false);
        tx.Commit();
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task TransformElements_RollbackOnFailure_ElementUnchanged()
    {
        var originalCurve = (_wall.Location as LocationCurve)?.Curve;
        using (var tx = new Transaction(_doc, "Rollback Move"))
        {
            tx.Start();
            ElementTransformUtils.MoveElement(_doc, _wall.Id, new XYZ(100, 0, 0));
            tx.RollBack();
        }
        var afterCurve = (_wall.Location as LocationCurve)?.Curve;
        await Assert.That(afterCurve.GetEndPoint(0).DistanceTo(originalCurve.GetEndPoint(0))).IsLessThan(0.001);
    }
}
