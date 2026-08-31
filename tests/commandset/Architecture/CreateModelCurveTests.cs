using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests.Architecture;

public class CreateModelCurveTests : RevitApiTest
{
    private static Document _doc;
    private static SketchPlane _sketchPlane;

    [Before(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Setup()
    {
        _doc = Application.NewProjectDocument(UnitSystem.Imperial);
        using var tx = new Transaction(_doc, "Setup");
        tx.Start();
        var level = Level.Create(_doc, 0.0);
        var plane = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, XYZ.Zero);
        _sketchPlane = SketchPlane.Create(_doc, plane);
        tx.Commit();
    }

    [After(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Cleanup() => _doc?.Close(false);

    [Test]
    public async Task CreateModelCurve_Line_CurveCreated()
    {
        using var tx = new Transaction(_doc, "Create Model Curve");
        tx.Start();
        var line = Line.CreateBound(new XYZ(0, 0, 0), new XYZ(10, 0, 0));
        var curve = _doc.Create.NewModelCurve(line, _sketchPlane);
        tx.Commit();
        await Assert.That(curve).IsNotNull();
    }

    [Test]
    public async Task CreateModelCurve_Arc_ArcCreated()
    {
        using var tx = new Transaction(_doc, "Create Arc Curve");
        tx.Start();
        var arc = Arc.Create(new XYZ(0, 0, 0), new XYZ(10, 0, 0), new XYZ(5, 5, 0));
        var curve = _doc.Create.NewModelCurve(arc, _sketchPlane);
        tx.Commit();
        await Assert.That(curve).IsNotNull();
    }

    [Test]
    public async Task CreateModelCurve_RollbackOnFailure_CurveNotPersisted()
    {
        int countBefore = new FilteredElementCollector(_doc).OfClass(typeof(CurveElement)).GetElementCount();
        using (var tx = new Transaction(_doc, "Rollback Curve"))
        {
            tx.Start();
            var line = Line.CreateBound(new XYZ(20, 0, 0), new XYZ(25, 0, 0));
            _doc.Create.NewModelCurve(line, _sketchPlane);
            tx.RollBack();
        }
        int countAfter = new FilteredElementCollector(_doc).OfClass(typeof(CurveElement)).GetElementCount();
        await Assert.That(countAfter).IsEqualTo(countBefore);
    }
}
