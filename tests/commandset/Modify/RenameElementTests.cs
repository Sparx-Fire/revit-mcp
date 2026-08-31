using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests.Modify;

public class RenameElementTests : RevitApiTest
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
        _level.Name = "Original Name";
        tx.Commit();
    }

    [After(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Cleanup() => _doc?.Close(false);

    [Test]
    public async Task RenameElement_Level_NameChanged()
    {
        using var tx = new Transaction(_doc, "Rename Level");
        tx.Start();
        _level.Name = "Renamed Level";
        tx.Commit();
        await Assert.That(_level.Name).IsEqualTo("Renamed Level");
    }

    [Test]
    public async Task RenameElement_RollbackOnFailure_NameUnchanged()
    {
        var originalName = _level.Name;
        using (var tx = new Transaction(_doc, "Rollback Rename"))
        {
            tx.Start();
            _level.Name = "Temporary Name";
            tx.RollBack();
        }
        await Assert.That(_level.Name).IsEqualTo(originalName);
    }
}
