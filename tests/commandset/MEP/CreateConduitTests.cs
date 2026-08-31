using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests.MEP;

public class CreateConduitTests : RevitApiTest
{
    private static Document _doc;
    private static Level _level;
    private static ConduitType _conduitType;

    [Before(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Setup()
    {
        _doc = Application.NewProjectDocument(UnitSystem.Imperial);
        using var tx = new Transaction(_doc, "Setup");
        tx.Start();
        _level = Level.Create(_doc, 0.0);
        _level.Name = "Test Level";
        _conduitType = new FilteredElementCollector(_doc)
            .OfClass(typeof(ConduitType))
            .Cast<ConduitType>()
            .FirstOrDefault();
        tx.Commit();
    }

    [After(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Cleanup() => _doc?.Close(false);

    [Test]
    public async Task CreateConduit_BetweenPoints_ConduitCreated()
    {
        using var tx = new Transaction(_doc, "Create Conduit");
        tx.Start();
#if REVIT2025_OR_GREATER
        var conduit = Conduit.Create(_doc, _conduitType?.Id ?? ElementId.InvalidElementId, new XYZ(0, 0, 0), new XYZ(10, 0, 0), _level.Id);
        tx.Commit();
        await Assert.That(conduit).IsNotNull();
#else
        tx.RollBack();
#endif
    }

    [Test]
    public async Task CreateConduit_RollbackOnFailure_ConduitNotPersisted()
    {
        int countBefore = new FilteredElementCollector(_doc).OfClass(typeof(Conduit)).GetElementCount();
        using (var tx = new Transaction(_doc, "Rollback Conduit"))
        {
            tx.Start();
#if REVIT2025_OR_GREATER
            Conduit.Create(_doc, _conduitType?.Id ?? ElementId.InvalidElementId, new XYZ(20, 0, 0), new XYZ(30, 0, 0), _level.Id);
#endif
            tx.RollBack();
        }
        int countAfter = new FilteredElementCollector(_doc).OfClass(typeof(Conduit)).GetElementCount();
        await Assert.That(countAfter).IsEqualTo(countBefore);
    }
}
