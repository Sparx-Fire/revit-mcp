using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests.Architecture;

public class CreateStairTests : RevitApiTest
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
    public async Task CreateStair_LevelsExist()
    {
        await Assert.That(_level1).IsNotNull();
        await Assert.That(_level2).IsNotNull();
    }
}
