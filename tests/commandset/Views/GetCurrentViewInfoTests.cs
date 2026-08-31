using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests.Views;

public class GetCurrentViewInfoTests : RevitApiTest
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
    public async Task GetCurrentViewInfo_ActiveView_ViewNotNull()
    {
        var view = _doc.ActiveView;
        await Assert.That(view).IsNotNull();
    }

    [Test]
    public async Task GetCurrentViewInfo_ViewType_ViewTypeReturned()
    {
        var view = _doc.ActiveView;
        await Assert.That(view.ViewType).IsEqualTo(ViewType.FloorPlan);
    }

    [Test]
    public async Task GetCurrentViewInfo_ViewName_NameNotEmpty()
    {
        var view = _doc.ActiveView;
        await Assert.That(string.IsNullOrEmpty(view.Name)).IsFalse();
    }
}
