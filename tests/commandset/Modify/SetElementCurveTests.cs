using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests.Modify;

public class SetElementCurveTests : RevitApiTest
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
    public async Task SetElementCurve_LocationCurve_CurveAccessible()
    {
        var locCurve = _wall.Location as LocationCurve;
        await Assert.That(locCurve).IsNotNull();
        var curve = locCurve.Curve;
        await Assert.That(curve).IsNotNull();
        await Assert.That(curve.Length).IsGreaterThan(0);
    }

    [Test]
    public async Task SetElementCurve_RollbackOnFailure_CurveUnchanged()
    {
        var originalEndPoint = (_wall.Location as LocationCurve)?.Curve.GetEndPoint(1);
        using (var tx = new Transaction(_doc, "Rollback Curve Change"))
        {
            tx.Start();
            var locCurve = _wall.Location as LocationCurve;
            if (locCurve != null)
                locCurve.Curve = Line.CreateBound(new XYZ(0, 0, 0), new XYZ(20, 0, 0));
            tx.RollBack();
        }
        var afterEndPoint = (_wall.Location as LocationCurve)?.Curve.GetEndPoint(1);
        await Assert.That(afterEndPoint.DistanceTo(originalEndPoint)).IsLessThan(0.001);
    }
}
