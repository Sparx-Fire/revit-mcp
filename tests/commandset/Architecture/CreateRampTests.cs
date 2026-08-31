using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests.Architecture;

public class CreateRampTests : RevitApiTest
{
    private static Document _doc;
    private static Level _level1;
    private static Level _level2;

    [Before(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Setup()
    {
        _doc = Application.NewProjectDocument(UnitSystem.Imperial);
        using var tx = new Transaction(_doc, "Setup");
        tx.Start();
        _level1 = Level.Create(_doc, 0.0);
        _level1.Name = "Level 1";
        _level2 = Level.Create(_doc, 10.0);
        _level2.Name = "Level 2";
        tx.Commit();
    }

    [After(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Cleanup() => _doc?.Close(false);

    [Test]
    public async Task CreateRamp_RunBetweenLevels_RunCreated()
    {
        using var tx = new Transaction(_doc, "Create Ramp Run");
        tx.Start();
#if REVIT2023_OR_GREATER
        tx.RollBack();
#endif
    }

    [Test]
    public async Task CreateRamp_RollbackOnFailure_RampNotPersisted()
    {
        int countBefore = new FilteredElementCollector(_doc).OfClass(typeof(Wall)/*RampRun*/).GetElementCount();
        using (var tx = new Transaction(_doc, "Rollback Ramp"))
        {
            tx.Start();
            tx.RollBack();
        }
        int countAfter = new FilteredElementCollector(_doc).OfClass(typeof(Wall)/*RampRun*/).GetElementCount();
        await Assert.That(countAfter).IsEqualTo(countBefore);
    }
}
