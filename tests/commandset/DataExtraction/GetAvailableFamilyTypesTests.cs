using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests.DataExtraction;

public class GetAvailableFamilyTypesTests : RevitApiTest
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
    public async Task GetAvailableFamilyTypes_WallTypes_TypesFound()
    {
        var types = new FilteredElementCollector(_doc)
            .OfClass(typeof(WallType))
            .Cast<WallType>()
            .ToList();
        await Assert.That(types.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task GetAvailableFamilyTypes_FloorTypes_TypesFound()
    {
        var types = new FilteredElementCollector(_doc)
            .OfClass(typeof(FloorType))
            .Cast<FloorType>()
            .ToList();
        await Assert.That(types.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task GetAvailableFamilyTypes_RoofTypes_TypesFound()
    {
        var types = new FilteredElementCollector(_doc)
            .OfClass(typeof(RoofType))
            .Cast<RoofType>()
            .ToList();
        await Assert.That(types.Count).IsGreaterThan(0);
    }
}
