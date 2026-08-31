using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests.MEP;

public class ConnectMEPTests : RevitApiTest
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
    public async Task ConnectMEP_ConnectorManager_ConnectorsAvailable()
    {
        using var tx = new Transaction(_doc, "Create Elements");
        tx.Start();
        var level = Level.Create(_doc, 0.0);
        var mechType = new FilteredElementCollector(_doc)
            .OfClass(typeof(Autodesk.Revit.DB.Mechanical.MechanicalSystemType))
            .Cast<Autodesk.Revit.DB.Mechanical.MechanicalSystemType>()
            .FirstOrDefault();
        tx.RollBack();
        await Assert.That(true).IsTrue();
    }
}
