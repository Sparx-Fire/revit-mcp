using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests.MEP;

public class CreateSpaceTests : RevitApiTest
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
    public async Task CreateSpace_AtPoint_SpaceCreated()
    {
        using var tx = new Transaction(_doc, "Create Space");
        tx.Start();
#if REVIT2025_OR_GREATER
        var space = _doc.Create.NewSpace(_level, new UV(5, 5));
        tx.Commit();
        await Assert.That(space).IsNotNull();
#else
        tx.RollBack();
#endif
    }

    [Test]
    public async Task CreateSpace_RollbackOnFailure_SpaceNotPersisted()
    {
        int countBefore = new FilteredElementCollector(_doc).OfClass(typeof(Space)).GetElementCount();
        using (var tx = new Transaction(_doc, "Rollback Space"))
        {
            tx.Start();
#if REVIT2025_OR_GREATER
            _doc.Create.NewSpace(_level, new UV(15, 5));
#endif
            tx.RollBack();
        }
        int countAfter = new FilteredElementCollector(_doc).OfClass(typeof(Space)).GetElementCount();
        await Assert.That(countAfter).IsEqualTo(countBefore);
    }
}
