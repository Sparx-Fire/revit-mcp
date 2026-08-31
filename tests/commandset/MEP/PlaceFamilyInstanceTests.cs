using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests.MEP;

public class PlaceFamilyInstanceTests : RevitApiTest
{
    private static Document _doc;
    private static Level _level;
    private static FamilySymbol _symbol;

    [Before(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Setup()
    {
        _doc = Application.NewProjectDocument(UnitSystem.Imperial);
        using var tx = new Transaction(_doc, "Setup");
        tx.Start();
        _level = Level.Create(_doc, 0.0);
        _level.Name = "Test Level";
        _symbol = new FilteredElementCollector(_doc)
            .OfClass(typeof(FamilySymbol))
            .Cast<FamilySymbol>()
            .FirstOrDefault();
        if (_symbol != null && !_symbol.IsActive)
            _symbol.Activate();
        tx.Commit();
    }

    [After(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Cleanup() => _doc?.Close(false);

    [Test]
    public async Task PlaceFamilyInstance_AtPoint_InstanceCreated()
    {
        if (_symbol == null) return;
        using var tx = new Transaction(_doc, "Place Instance");
        tx.Start();
        var instance = _doc.Create.NewFamilyInstance(new XYZ(5, 5, 0), _symbol, _level, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
        tx.Commit();
        await Assert.That(instance).IsNotNull();
    }

    [Test]
    public async Task PlaceFamilyInstance_RollbackOnFailure_InstanceNotPersisted()
    {
        if (_symbol == null) return;
        int countBefore = new FilteredElementCollector(_doc).OfClass(typeof(FamilyInstance)).GetElementCount();
        using (var tx = new Transaction(_doc, "Rollback Instance"))
        {
            tx.Start();
            _doc.Create.NewFamilyInstance(new XYZ(15, 5, 0), _symbol, _level, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
            tx.RollBack();
        }
        int countAfter = new FilteredElementCollector(_doc).OfClass(typeof(FamilyInstance)).GetElementCount();
        await Assert.That(countAfter).IsEqualTo(countBefore);
    }
}
