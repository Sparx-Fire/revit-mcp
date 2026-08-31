using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests.MEP;

public class CreateMEPSystemTests : RevitApiTest
{
    private static Document _doc;
    private static MechanicalSystemType _supplyAirType;

    [Before(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Setup()
    {
        _doc = Application.NewProjectDocument(UnitSystem.Imperial);
        using var tx = new Transaction(_doc, "Setup");
        tx.Start();
        _supplyAirType = new FilteredElementCollector(_doc)
            .OfClass(typeof(MechanicalSystemType))
            .Cast<MechanicalSystemType>()
            .FirstOrDefault();
        tx.Commit();
    }

    [After(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Cleanup() => _doc?.Close(false);

    [Test]
    public async Task CreateMEPSystem_SupplyAir_SystemCreated()
    {
        using var tx = new Transaction(_doc, "Create MEP System");
        tx.Start();
        var mepSystem = MechanicalSystem.Create(_doc, _supplyAirType?.Id ?? ElementId.InvalidElementId);
        tx.Commit();
        await Assert.That(mepSystem).IsNotNull();
    }

    [Test]
    public async Task CreateMEPSystem_RollbackOnFailure_SystemNotPersisted()
    {
        int countBefore = new FilteredElementCollector(_doc).OfClass(typeof(MEPSystem)).GetElementCount();
        using (var tx = new Transaction(_doc, "Rollback System"))
        {
            tx.Start();
            MechanicalSystem.Create(_doc, _supplyAirType?.Id ?? ElementId.InvalidElementId);
            tx.RollBack();
        }
        int countAfter = new FilteredElementCollector(_doc).OfClass(typeof(MEPSystem)).GetElementCount();
        await Assert.That(countAfter).IsEqualTo(countBefore);
    }
}
