using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests.Modify;

public class DuplicateTypeTests : RevitApiTest
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
    public async Task DuplicateType_WallType_DuplicateCreated()
    {
        using var tx = new Transaction(_doc, "Duplicate Type");
        tx.Start();
        var original = new FilteredElementCollector(_doc)
            .OfClass(typeof(WallType))
            .Cast<WallType>()
            .First();
        var newType = original.Duplicate("My Custom Wall Type") as WallType;
        tx.Commit();
        await Assert.That(newType).IsNotNull();
        await Assert.That(newType.Name).IsEqualTo("My Custom Wall Type");
    }

    [Test]
    public async Task DuplicateType_RollbackOnFailure_TypeNotPersisted()
    {
        int countBefore = new FilteredElementCollector(_doc).OfClass(typeof(WallType)).GetElementCount();
        using (var tx = new Transaction(_doc, "Rollback Duplicate"))
        {
            tx.Start();
            var original = new FilteredElementCollector(_doc)
                .OfClass(typeof(WallType))
                .Cast<WallType>()
                .First();
            original.Duplicate("Rollback Type");
            tx.RollBack();
        }
        int countAfter = new FilteredElementCollector(_doc).OfClass(typeof(WallType)).GetElementCount();
        await Assert.That(countAfter).IsEqualTo(countBefore);
    }
}
