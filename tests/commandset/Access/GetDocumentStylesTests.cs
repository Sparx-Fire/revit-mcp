using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using RevitMCPCommandSet.Utils;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests.Access;

public class GetDocumentStylesTests : RevitApiTest
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
    public static void Cleanup()
    {
        _doc?.Close(false);
    }

    [Test]
    public async Task DimensionTypes_AreAvailableInProject()
    {
        var dimensionTypes = new FilteredElementCollector(_doc)
            .OfClass(typeof(DimensionType))
            .Cast<DimensionType>()
            .ToList();

        await Assert.That(dimensionTypes.Count).IsGreaterThan(0);
        await Assert.That(dimensionTypes.All(type => !string.IsNullOrWhiteSpace(type.Name))).IsTrue();
    }

    [Test]
    public async Task GridTypes_AreAvailableInProject()
    {
        var gridTypes = new FilteredElementCollector(_doc)
            .OfClass(typeof(GridType))
            .Cast<GridType>()
            .ToList();

        await Assert.That(gridTypes.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task TextNoteTypes_ExposeTextHeightInMillimeters()
    {
        var textNoteType = new FilteredElementCollector(_doc)
            .OfClass(typeof(TextNoteType))
            .Cast<TextNoteType>()
            .FirstOrDefault();

        await Assert.That(textNoteType).IsNotNull();

        var textSizeParam = textNoteType!.get_Parameter(BuiltInParameter.TEXT_SIZE);
        await Assert.That(textSizeParam).IsNotNull();
        await Assert.That(textSizeParam!.HasValue).IsTrue();

        var textHeightMm = RevitUnitConversion.ToMillimeters(textSizeParam.AsDouble());
        await Assert.That(textHeightMm).IsGreaterThan(0);
    }

    [Test]
    public async Task LinePatterns_AreAvailableInProject()
    {
        var linePatterns = new FilteredElementCollector(_doc)
            .OfClass(typeof(LinePatternElement))
            .Cast<LinePatternElement>()
            .ToList();

        await Assert.That(linePatterns.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task GraphicsStyles_WithNames_AreAvailableInProject()
    {
        var graphicsStyles = new FilteredElementCollector(_doc)
            .OfClass(typeof(GraphicsStyle))
            .Cast<GraphicsStyle>()
            .Where(style => !string.IsNullOrWhiteSpace(style.Name))
            .ToList();

        await Assert.That(graphicsStyles.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task TitleBlocks_CollectorReturnsSymbolsOrEmptyList()
    {
        var titleBlocks = new FilteredElementCollector(_doc)
            .OfCategory(BuiltInCategory.OST_TitleBlocks)
            .OfClass(typeof(FamilySymbol))
            .Cast<FamilySymbol>()
            .ToList();

        await Assert.That(titleBlocks.Count).IsGreaterThanOrEqualTo(0);

        if (titleBlocks.Count > 0)
        {
            await Assert.That(titleBlocks.All(symbol =>
                !string.IsNullOrWhiteSpace(symbol.FamilyName) &&
                !string.IsNullOrWhiteSpace(symbol.Name))).IsTrue();
        }
    }
}
