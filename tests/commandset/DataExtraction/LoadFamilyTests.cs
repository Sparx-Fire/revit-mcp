using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests.DataExtraction;

public class LoadFamilyTests : RevitApiTest
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
    public async Task LoadFamily_ListFamilies_FamiliesFound()
    {
        var families = new FilteredElementCollector(_doc)
            .OfClass(typeof(Family))
            .Cast<Family>()
            .ToList();
        await Assert.That(families.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task LoadFamily_ListFamilySymbols_SymbolsFound()
    {
        var symbols = new FilteredElementCollector(_doc)
            .OfClass(typeof(FamilySymbol))
            .Cast<FamilySymbol>()
            .ToList();
        await Assert.That(symbols.Count).IsGreaterThan(0);
    }
}
