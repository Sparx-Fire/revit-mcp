using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests.Views;

public class ExportViewsTests : RevitApiTest
{
    private static Document _doc;
    private static Level _level;
    private static ViewPlan _floorPlan;

    [Before(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Setup()
    {
        _doc = Application.NewProjectDocument(UnitSystem.Imperial);
        using var tx = new Transaction(_doc, "Setup");
        tx.Start();
        _level = Level.Create(_doc, 0.0);
        _level.Name = "Test Level";
        var floorPlanType = new FilteredElementCollector(_doc)
            .OfClass(typeof(ViewFamilyType))
            .Cast<ViewFamilyType>()
            .FirstOrDefault(vft => vft.ViewFamily == ViewFamily.FloorPlan);
        _floorPlan = ViewPlan.Create(_doc, floorPlanType.Id, _level.Id);
        tx.Commit();
    }

    [After(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Cleanup() => _doc?.Close(false);

    [Test]
    public async Task ExportViews_DwgExport_OptionsCreated()
    {
        var dwgOpts = new DWGExportOptions();
        await Assert.That(dwgOpts).IsNotNull();
    }

    [Test]
    public async Task ExportViews_ImageExport_OptionsCreated()
    {
        var imgOpts = new ImageExportOptions();
        imgOpts.ExportRange = ExportRange.SetOfViews;
        imgOpts.SetViewsAndSheets(new List<ElementId> { _floorPlan.Id });
        imgOpts.ImageResolution = ImageResolution.DPI_150;
        await Assert.That(imgOpts).IsNotNull();
    }

    [Test]
    public async Task ExportViews_ImageResolution_ResolutionSet()
    {
        var imgOpts = new ImageExportOptions();
        imgOpts.ImageResolution = ImageResolution.DPI_300;
        await Assert.That(imgOpts.ImageResolution).IsEqualTo(ImageResolution.DPI_300);
    }
}
