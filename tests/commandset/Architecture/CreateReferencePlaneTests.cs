using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests.Architecture;

public class CreateReferencePlaneTests : RevitApiTest
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
    public async Task CreateReferencePlane_ByBubbleEnd_PlaneCreated()
    {
        using var tx = new Transaction(_doc, "Create Ref Plane");
        tx.Start();
        var view = _doc.ActiveView;
        var refPlane = _doc.Create.NewReferencePlane(new XYZ(0, 0, 0), new XYZ(10, 0, 0), XYZ.BasisZ, view);
        tx.Commit();
        await Assert.That(refPlane).IsNotNull();
    }

    [Test]
    public async Task CreateReferencePlane_RollbackOnFailure_PlaneNotPersisted()
    {
        int countBefore = new FilteredElementCollector(_doc).OfClass(typeof(ReferencePlane)).GetElementCount();
        using (var tx = new Transaction(_doc, "Rollback Ref Plane"))
        {
            tx.Start();
            var view = _doc.ActiveView;
            _doc.Create.NewReferencePlane(new XYZ(20, 0, 0), new XYZ(30, 0, 0), XYZ.BasisZ, view);
            tx.RollBack();
        }
        int countAfter = new FilteredElementCollector(_doc).OfClass(typeof(ReferencePlane)).GetElementCount();
        await Assert.That(countAfter).IsEqualTo(countBefore);
    }
}
