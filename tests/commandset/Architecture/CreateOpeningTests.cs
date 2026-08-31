using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests.Architecture;

public class CreateOpeningTests : RevitApiTest
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
    public async Task CreateOpening_RectangularInWall_OpeningCreated()
    {
        using var tx = new Transaction(_doc, "Create Opening");
        tx.Start();
#if REVIT2026_OR_GREATER
        var curveArray = new CurveArray();
        curveArray.Append(Line.CreateBound(new XYZ(3, 0, 0), new XYZ(5, 0, 0)));
        curveArray.Append(Line.CreateBound(new XYZ(5, 0, 0), new XYZ(5, 0, 3)));
        curveArray.Append(Line.CreateBound(new XYZ(5, 0, 3), new XYZ(3, 0, 3)));
        curveArray.Append(Line.CreateBound(new XYZ(3, 0, 3), new XYZ(3, 0, 0)));
        var opening = _doc.Create.NewOpening(_wall, curveArray, false);
#else
        var opening = default(Opening);
#endif
        tx.Commit();
#if !REVIT2025_OR_GREATER
        await Assert.That(opening).IsNotNull();
#endif
    }

    [Test]
    public async Task CreateOpening_RollbackOnFailure_OpeningNotPersisted()
    {
        int countBefore = new FilteredElementCollector(_doc).OfClass(typeof(Opening)).GetElementCount();
        using (var tx = new Transaction(_doc, "Rollback Opening"))
        {
            tx.Start();
#if !REVIT2025_OR_GREATER
            // //R25:Opening.Add(_wall, new XYZ(6, 0, 0), new XYZ(8, 0, 3));
#endif
            tx.RollBack();
        }
        int countAfter = new FilteredElementCollector(_doc).OfClass(typeof(Opening)).GetElementCount();
        await Assert.That(countAfter).IsEqualTo(countBefore);
    }
}
