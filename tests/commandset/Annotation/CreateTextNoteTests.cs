using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests.Annotation;

public class CreateTextNoteTests : RevitApiTest
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
    public async Task CreateTextNote_AtPoint_TextNoteCreated()
    {
        using var tx = new Transaction(_doc, "Create Text Note");
        tx.Start();
        var textNote = TextNote.Create(_doc, _floorPlan.Id, new XYZ(5, 5, 0), "Hello World", _floorPlan.Document.GetDefaultElementTypeId(ElementTypeGroup.TextNoteType));
        tx.Commit();
        await Assert.That(textNote).IsNotNull();
        await Assert.That(textNote.Text).IsEqualTo("Hello World");
    }

    [Test]
    public async Task CreateTextNote_RollbackOnFailure_TextNoteNotPersisted()
    {
        int countBefore = new FilteredElementCollector(_doc).OfClass(typeof(TextNote)).GetElementCount();
        using (var tx = new Transaction(_doc, "Rollback Text Note"))
        {
            tx.Start();
            TextNote.Create(_doc, _floorPlan.Id, new XYZ(10, 10, 0), "Rollback", _floorPlan.Document.GetDefaultElementTypeId(ElementTypeGroup.TextNoteType));
            tx.RollBack();
        }
        int countAfter = new FilteredElementCollector(_doc).OfClass(typeof(TextNote)).GetElementCount();
        await Assert.That(countAfter).IsEqualTo(countBefore);
    }
}
