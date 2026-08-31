using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests.Views;

public class CreateFilledRegionTests : RevitApiTest
{
    private static Document _doc;
    private static View _view;

    [Before(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Setup()
    {
        _doc = Application.NewProjectDocument(UnitSystem.Imperial);
        _view = _doc.ActiveView;
    }

    [After(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Cleanup() => _doc?.Close(false);

    [Test]
    public async Task CreateFilledRegion_FilledRegionType_TypeFound()
    {
        var types = new FilteredElementCollector(_doc)
            .OfClass(typeof(FilledRegionType))
            .Cast<FilledRegionType>()
            .ToList();
        await Assert.That(types.Count).IsGreaterThan(0);
    }
}
