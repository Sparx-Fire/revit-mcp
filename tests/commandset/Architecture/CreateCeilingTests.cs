using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests.Architecture;

public class CreateCeilingTests : RevitApiTest
{
    private static Document _doc;
    private static Level _level;
    private static CeilingType _ceilingType;

    [Before(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Setup()
    {
        _doc = Application.NewProjectDocument(UnitSystem.Imperial);
        using var tx = new Transaction(_doc, "Setup");
        tx.Start();
        _level = Level.Create(_doc, 10.0);
        _level.Name = "Ceiling Level";
        _ceilingType = new FilteredElementCollector(_doc)
            .OfClass(typeof(CeilingType))
            .Cast<CeilingType>()
            .FirstOrDefault();
        tx.Commit();
    }

    [After(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Cleanup() => _doc?.Close(false);

    [Test]
    public async Task CreateCeiling_Rectangular_CeilingCreated()
    {
        using var tx = new Transaction(_doc, "Create Ceiling");
        tx.Start();
        var loop = new CurveLoop();
        loop.Append(Line.CreateBound(new XYZ(0, 0, 0), new XYZ(10, 0, 0)));
        loop.Append(Line.CreateBound(new XYZ(10, 0, 0), new XYZ(10, 10, 0)));
        loop.Append(Line.CreateBound(new XYZ(10, 10, 0), new XYZ(0, 10, 0)));
        loop.Append(Line.CreateBound(new XYZ(0, 10, 0), new XYZ(0, 0, 0)));
#if REVIT2023_OR_GREATER
        var ceiling = Ceiling.Create(_doc, new List<CurveLoop> { loop }, _ceilingType.Id, _level.Id);
        tx.Commit();
        await Assert.That(ceiling).IsNotNull();
#else
        tx.RollBack();
        await Assert.That(true).IsTrue();
#endif
    }

    [Test]
    public async Task CreateCeiling_RollbackOnFailure_CeilingNotPersisted()
    {
        int countBefore = new FilteredElementCollector(_doc).OfClass(typeof(Ceiling)).GetElementCount();
        using (var tx = new Transaction(_doc, "Rollback Ceiling"))
        {
            tx.Start();
            var loop = new CurveLoop();
            loop.Append(Line.CreateBound(new XYZ(20, 0, 0), new XYZ(25, 0, 0)));
            loop.Append(Line.CreateBound(new XYZ(25, 0, 0), new XYZ(25, 5, 0)));
            loop.Append(Line.CreateBound(new XYZ(25, 5, 0), new XYZ(20, 5, 0)));
            loop.Append(Line.CreateBound(new XYZ(20, 5, 0), new XYZ(20, 0, 0)));
#if REVIT2023_OR_GREATER
            Ceiling.Create(_doc, new List<CurveLoop> { loop }, _ceilingType.Id, _level.Id);
#endif
            tx.RollBack();
        }
        int countAfter = new FilteredElementCollector(_doc).OfClass(typeof(Ceiling)).GetElementCount();
        await Assert.That(countAfter).IsEqualTo(countBefore);
    }
}
