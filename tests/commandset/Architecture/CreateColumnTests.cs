using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests.Architecture;

public class CreateColumnTests : RevitApiTest
{
    private static Document _doc;
    private static Level _level;
    private static FamilySymbol _columnSymbol;

    [Before(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Setup()
    {
        _doc = Application.NewProjectDocument(UnitSystem.Imperial);
        using var tx = new Transaction(_doc, "Setup");
        tx.Start();
        _level = Level.Create(_doc, 0.0);
        _level.Name = "Test Level";
        _columnSymbol = new FilteredElementCollector(_doc)
            .OfClass(typeof(FamilySymbol))
            .Cast<FamilySymbol>()
            .FirstOrDefault(fs => fs.Category?.BuiltInCategory == BuiltInCategory.OST_StructuralColumns);
        if (_columnSymbol != null && !_columnSymbol.IsActive)
            _columnSymbol.Activate();
        tx.Commit();
    }

    [After(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Cleanup() => _doc?.Close(false);

    [Test]
    public async Task CreateColumn_AtLocation_ColumnCreated()
    {
        if (_columnSymbol == null) return;
        using var tx = new Transaction(_doc, "Create Column");
        tx.Start();
        var column = _doc.Create.NewFamilyInstance(new XYZ(5, 5, 0), _columnSymbol, _level, StructuralType.Column);
        tx.Commit();
        await Assert.That(column).IsNotNull();
    }

    [Test]
    public async Task CreateColumn_RollbackOnFailure_ColumnNotPersisted()
    {
        if (_columnSymbol == null) return;
        int countBefore = new FilteredElementCollector(_doc)
            .OfClass(typeof(FamilyInstance))
            .GetElementCount();
        using (var tx = new Transaction(_doc, "Rollback Column"))
        {
            tx.Start();
            _doc.Create.NewFamilyInstance(new XYZ(15, 5, 0), _columnSymbol, _level, StructuralType.Column);
            tx.RollBack();
        }
        int countAfter = new FilteredElementCollector(_doc)
            .OfClass(typeof(FamilyInstance))
            .GetElementCount();
        await Assert.That(countAfter).IsEqualTo(countBefore);
    }
}
