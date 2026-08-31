using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests.Views;

public class CreateDetailCurveTests : RevitApiTest
{
    private static Document _doc;
    private static Level _level;
    private static ViewPlan _floorPlan;

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
        tx.Commit();
    }

    [After(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Cleanup() => _doc?.Close(false);

    [Test]
    public async Task CreateDetailCurve_Line_DetailCurveCreated()
    {
        using var tx = new Transaction(_doc, "Create Detail Curve");
        tx.Start();
        var line = Line.CreateBound(new XYZ(0, 0, 0), new XYZ(10, 0, 0));
        var curve = _doc.Create.NewDetailCurve(_floorPlan, line);
        tx.Commit();
        await Assert.That(curve).IsNotNull();
    }

    [Test]
    public async Task CreateDetailCurve_RollbackOnFailure_CurveNotPersisted()
    {
        int countBefore = new FilteredElementCollector(_doc).OfClass(typeof(DetailCurve)).GetElementCount();
        using (var tx = new Transaction(_doc, "Rollback Detail Curve"))
        {
            tx.Start();
            _doc.Create.NewDetailCurve(_floorPlan, Line.CreateBound(new XYZ(20, 0, 0), new XYZ(30, 0, 0)));
            tx.RollBack();
        }
        int countAfter = new FilteredElementCollector(_doc).OfClass(typeof(DetailCurve)).GetElementCount();
        await Assert.That(countAfter).IsEqualTo(countBefore);
    }
}
