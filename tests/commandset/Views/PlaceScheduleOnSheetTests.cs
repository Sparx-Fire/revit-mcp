using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests.Views;

public class PlaceScheduleOnSheetTests : RevitApiTest
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
    public async Task PlaceScheduleOnSheet_CreateSchedule_ScheduleExists()
    {
        using var tx = new Transaction(_doc, "Create Schedule");
        tx.Start();
        var wallCategory = new FilteredElementCollector(_doc).OfClass(typeof(WallType)).FirstElement()?.Category;
        if (wallCategory != null)
        {
            var schedule = ViewSchedule.CreateSchedule(_doc, wallCategory.Id);
            tx.Commit();
            await Assert.That(schedule).IsNotNull();
        }
        else
        {
            tx.RollBack();
        }
    }
}
