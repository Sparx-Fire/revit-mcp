using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests.Views;

public class CreateDraftingViewTests : RevitApiTest
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
    public async Task CreateDraftingView_DraftingView_ViewCreated()
    {
        using var tx = new Transaction(_doc, "Create Drafting View");
        tx.Start();
        var vft = new FilteredElementCollector(_doc)
            .OfClass(typeof(ViewFamilyType))
            .Cast<ViewFamilyType>()
            .FirstOrDefault(vftype => vftype.ViewFamily == ViewFamily.Drafting);
        if (vft != null)
        {
            var view = ViewDrafting.Create(_doc, vft.Id);
            tx.Commit();
            await Assert.That(view).IsNotNull();
            await Assert.That(view.ViewType).IsEqualTo(ViewType.DraftingView);
        }
        else
        {
            tx.RollBack();
        }
    }

    [Test]
    public async Task CreateDraftingView_SetName_NameApplied()
    {
        using var tx = new Transaction(_doc, "Create Named Drafting View");
        tx.Start();
        var vft = new FilteredElementCollector(_doc)
            .OfClass(typeof(ViewFamilyType))
            .Cast<ViewFamilyType>()
            .FirstOrDefault(vftype => vftype.ViewFamily == ViewFamily.Drafting);
        if (vft != null)
        {
            var view = ViewDrafting.Create(_doc, vft.Id);
            view.Name = "My Detail";
            tx.Commit();
            await Assert.That(view.Name).IsEqualTo("My Detail");
        }
        else
        {
            tx.RollBack();
        }
    }
}
