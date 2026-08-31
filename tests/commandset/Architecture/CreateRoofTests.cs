using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests.Architecture;

public class CreateRoofTests : RevitApiTest
{
    private static Document _doc;
    private static Level _level;
    private static RoofType _roofType;

    [Before(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Setup()
    {
        _doc = Application.NewProjectDocument(UnitSystem.Imperial);
        using var tx = new Transaction(_doc, "Setup");
        tx.Start();
        _level = Level.Create(_doc, 0.0);
        _level.Name = "Ground Level";
        var topLevel = Level.Create(_doc, 10.0);
        topLevel.Name = "Top Level";
        _roofType = new FilteredElementCollector(_doc)
            .OfClass(typeof(RoofType))
            .Cast<RoofType>()
            .FirstOrDefault();
        tx.Commit();
    }

    [After(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Cleanup() => _doc?.Close(false);

    [Test]
    public async Task CreateRoof_FootprintRoof_RoofCreated()
    {
        using var tx = new Transaction(_doc, "Create Roof");
        tx.Start();
        var curveArray = new CurveArray();
        curveArray.Append(Line.CreateBound(new XYZ(0, 0, 0), new XYZ(20, 0, 0)));
        curveArray.Append(Line.CreateBound(new XYZ(20, 0, 0), new XYZ(20, 15, 0)));
        curveArray.Append(Line.CreateBound(new XYZ(20, 15, 0), new XYZ(0, 15, 0)));
        curveArray.Append(Line.CreateBound(new XYZ(0, 15, 0), new XYZ(0, 0, 0)));
        var roof = _doc.Create.NewFootPrintRoof(curveArray, _level, _roofType, out _);
        tx.Commit();
        await Assert.That(roof).IsNotNull();
    }

    [Test]
    public async Task CreateRoof_RollbackOnFailure_RoofNotPersisted()
    {
        int countBefore = new FilteredElementCollector(_doc).OfClass(typeof(RoofBase)).GetElementCount();
        using (var tx = new Transaction(_doc, "Rollback Roof"))
        {
            tx.Start();
            var curveArray = new CurveArray();
            curveArray.Append(Line.CreateBound(new XYZ(30, 0, 0), new XYZ(35, 0, 0)));
            curveArray.Append(Line.CreateBound(new XYZ(35, 0, 0), new XYZ(35, 5, 0)));
            curveArray.Append(Line.CreateBound(new XYZ(35, 5, 0), new XYZ(30, 5, 0)));
            curveArray.Append(Line.CreateBound(new XYZ(30, 5, 0), new XYZ(30, 0, 0)));
            _doc.Create.NewFootPrintRoof(curveArray, _level, _roofType, out _);
            tx.RollBack();
        }
        int countAfter = new FilteredElementCollector(_doc).OfClass(typeof(RoofBase)).GetElementCount();
        await Assert.That(countAfter).IsEqualTo(countBefore);
    }
}
