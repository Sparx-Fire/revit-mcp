using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests.Architecture;

public class CreateRailingTests : RevitApiTest
{
    private static Document _doc;
    private static Level _level;
    private static RailingType _railingType;

    [Before(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Setup()
    {
        _doc = Application.NewProjectDocument(UnitSystem.Imperial);
        using var tx = new Transaction(_doc, "Setup");
        tx.Start();
        _level = Level.Create(_doc, 0.0);
        _level.Name = "Test Level";
        _railingType = new FilteredElementCollector(_doc)
            .OfClass(typeof(RailingType))
            .Cast<RailingType>()
            .FirstOrDefault();
        tx.Commit();
    }

    [After(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Cleanup() => _doc?.Close(false);

    [Test]
    public async Task CreateRailing_AlongLine_RailingCreated()
    {
        if (_railingType == null) return;
        using var tx = new Transaction(_doc, "Create Railing");
        tx.Start();
        var line = Line.CreateBound(new XYZ(0, 0, 0), new XYZ(10, 0, 0));
#if !REVIT2025_OR_GREATER
        var railing = Railing.Create(_doc, line, _railingType.Id, _level.Id);
#else
        var loop = new CurveLoop();
        loop.Append(line);
        var railing = Railing.Create(_doc, loop, _railingType.Id, _level.Id);
#endif
        tx.Commit();
        await Assert.That(railing).IsNotNull();
    }

    [Test]
    public async Task CreateRailing_RollbackOnFailure_RailingNotPersisted()
    {
        if (_railingType == null) return;
        int countBefore = new FilteredElementCollector(_doc).OfClass(typeof(Railing)).GetElementCount();
        using (var tx = new Transaction(_doc, "Rollback Railing"))
        {
            tx.Start();
            var line = Line.CreateBound(new XYZ(20, 0, 0), new XYZ(25, 0, 0));
#if !REVIT2025_OR_GREATER
            Railing.Create(_doc, line, _railingType.Id, _level.Id);
#else
            var loop = new CurveLoop();
            loop.Append(line);
            Railing.Create(_doc, loop, _railingType.Id, _level.Id);
#endif
            tx.RollBack();
        }
        int countAfter = new FilteredElementCollector(_doc).OfClass(typeof(Railing)).GetElementCount();
        await Assert.That(countAfter).IsEqualTo(countBefore);
    }
}
