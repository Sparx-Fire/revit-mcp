using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests;

public class SaveDocumentTests : RevitApiTest
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
    public async Task SaveDocument_IsModified_ModifiedFlagSet()
    {
        using var tx = new Transaction(_doc, "Modify");
        tx.Start();
        Level.Create(_doc, 10.0);
        tx.Commit();
        await Assert.That(_doc.IsModified).IsTrue();
    }

    [Test]
    public async Task SaveDocument_Path_ValidPath()
    {
        var path = _doc.PathName;
        await Assert.That(path).IsNotNull();
    }
}
