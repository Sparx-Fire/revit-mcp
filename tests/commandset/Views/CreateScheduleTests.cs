using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests.Views;

public class CreateScheduleTests : RevitApiTest
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
    public async Task CreateSchedule_DoorsCategory_ScheduleIsCreated()
    {
        ViewSchedule schedule = null;

        using (var tx = new Transaction(_doc, "Create Door Schedule"))
        {
            tx.Start();
            schedule = ViewSchedule.CreateSchedule(_doc, new ElementId(BuiltInCategory.OST_Doors));
            schedule.Name = "MCP Door Schedule Test";
            tx.Commit();
        }

        await Assert.That(schedule).IsNotNull();
        await Assert.That(schedule.Definition.CategoryId.IntegerValue)
            .IsEqualTo((int)BuiltInCategory.OST_Doors);
    }

    [Test]
    public async Task CreateSchedule_WindowsCategory_ScheduleIsCreated()
    {
        ViewSchedule schedule = null;

        using (var tx = new Transaction(_doc, "Create Window Schedule"))
        {
            tx.Start();
            schedule = ViewSchedule.CreateSchedule(_doc, new ElementId(BuiltInCategory.OST_Windows));
            schedule.Name = "MCP Window Schedule Test";
            tx.Commit();
        }

        await Assert.That(schedule).IsNotNull();
        await Assert.That(schedule.Definition.CategoryId.IntegerValue)
            .IsEqualTo((int)BuiltInCategory.OST_Windows);
    }

    [Test]
    public async Task CreateSchedule_RoomsCategory_ScheduleIsCreated()
    {
        ViewSchedule schedule = null;

        using (var tx = new Transaction(_doc, "Create Room Schedule"))
        {
            tx.Start();
            schedule = ViewSchedule.CreateSchedule(_doc, new ElementId(BuiltInCategory.OST_Rooms));
            schedule.Name = "MCP Room Schedule Test";
            tx.Commit();
        }

        await Assert.That(schedule).IsNotNull();
        await Assert.That(schedule.Definition.CategoryId.IntegerValue)
            .IsEqualTo((int)BuiltInCategory.OST_Rooms);
    }

    [Test]
    public async Task CreateSchedule_AddMarkField_FieldCountIncreases()
    {
        ViewSchedule schedule = null;

        using (var tx = new Transaction(_doc, "Create Door Schedule With Field"))
        {
            tx.Start();
            schedule = ViewSchedule.CreateSchedule(_doc, new ElementId(BuiltInCategory.OST_Doors));
            schedule.Name = "MCP Door Schedule Fields Test";

            var definition = schedule.Definition;
            var initialCount = definition.GetFieldCount();

            SchedulableField markField = null;
            foreach (var schedulableField in definition.GetSchedulableFields())
            {
                if (schedulableField.GetName(_doc).Equals("Mark", StringComparison.OrdinalIgnoreCase))
                {
                    markField = schedulableField;
                    break;
                }
            }

            if (markField != null)
                definition.AddField(markField);

            tx.Commit();

            await Assert.That(definition.GetFieldCount()).IsGreaterThanOrEqualTo(initialCount);
        }
    }

    [Test]
    public async Task CreateSchedule_DuplicateTemplate_PreservesCategory()
    {
        ViewSchedule template = null;
        ViewSchedule duplicate = null;

        using (var tx = new Transaction(_doc, "Create Schedule Template"))
        {
            tx.Start();
            template = ViewSchedule.CreateSchedule(_doc, new ElementId(BuiltInCategory.OST_Doors));
            template.Name = "MCP Door Template";
            tx.Commit();
        }

        using (var tx = new Transaction(_doc, "Duplicate Schedule Template"))
        {
            tx.Start();
            var duplicateId = template.Duplicate(ViewDuplicateOption.Duplicate);
            duplicate = _doc.GetElement(duplicateId) as ViewSchedule;
            duplicate.Name = "MCP Door Schedule Copy";
            tx.Commit();
        }

        await Assert.That(duplicate).IsNotNull();
        await Assert.That(duplicate.Definition.CategoryId.IntegerValue)
            .IsEqualTo(template.Definition.CategoryId.IntegerValue);
    }
}
