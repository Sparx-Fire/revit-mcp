using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests.Views;

public class ManageScheduleFieldsTests : RevitApiTest
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
    public async Task ManageScheduleFields_AddField_FieldAdded()
    {
        using var tx = new Transaction(_doc, "Create Schedule With Field");
        tx.Start();
        var wallCategory = new FilteredElementCollector(_doc).OfClass(typeof(WallType)).FirstElement()?.Category;
        if (wallCategory != null)
        {
            var schedule = ViewSchedule.CreateSchedule(_doc, wallCategory.Id);
            var definition = schedule.Definition;
            var field = definition.AddField(ScheduleFieldType.Instance);
            tx.Commit();
            await Assert.That(field).IsNotNull();
        }
        else
        {
            tx.RollBack();
        }
    }

    [Test]
    public async Task ManageScheduleFields_GetFields_FieldsFound()
    {
        using var tx = new Transaction(_doc, "Create Schedule");
        tx.Start();
        var wallCategory = new FilteredElementCollector(_doc).OfClass(typeof(WallType)).FirstElement()?.Category;
        if (wallCategory != null)
        {
            var schedule = ViewSchedule.CreateSchedule(_doc, wallCategory.Id);
            var fieldCount = schedule.Definition.GetFieldCount();
            tx.Commit();
            await Assert.That(fieldCount).IsGreaterThan(0);
        }
        else
        {
            tx.RollBack();
        }
    }
}
