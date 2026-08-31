using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests.Architecture;

public class CreateLineElementTests : RevitApiTest
{
    private static Document _doc;
    private static Level _level;

    [Before(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Setup()
    {
        _doc = Application.NewProjectDocument(UnitSystem.Imperial);
        using var tx = new Transaction(_doc, "Setup");
        tx.Start();
        _level = Level.Create(_doc, 0.0);
        _level.Name = "Test Level";
        tx.Commit();
    }

    [After(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Cleanup() => _doc?.Close(false);

    [Test]
    public async Task CreateLineElement_DetailLine_LineCreated()
    {
        using var tx = new Transaction(_doc, "Create Detail Line");
        tx.Start();
        var view = _doc.ActiveView;
        var line = Line.CreateBound(new XYZ(0, 0, 0), new XYZ(10, 0, 0));
        var detailLine = _doc.Create.NewDetailCurve(view, line);
        tx.Commit();
        await Assert.That(detailLine).IsNotNull();
    }

    [Test]
    public async Task CreateLineElement_RollbackOnFailure_LineNotPersisted()
    {
        int countBefore = new FilteredElementCollector(_doc).OfClass(typeof(DetailCurve)).GetElementCount();
        using (var tx = new Transaction(_doc, "Rollback Line"))
        {
            tx.Start();
            var view = _doc.ActiveView;
            _doc.Create.NewDetailCurve(view, Line.CreateBound(new XYZ(20, 0, 0), new XYZ(30, 0, 0)));
            tx.RollBack();
        }
        int countAfter = new FilteredElementCollector(_doc).OfClass(typeof(DetailCurve)).GetElementCount();
        await Assert.That(countAfter).IsEqualTo(countBefore);
    }
}
