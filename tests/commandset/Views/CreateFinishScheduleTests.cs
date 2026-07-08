using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests.Views;

public class CreateFinishScheduleTests : RevitApiTest
{
    private static Document _doc;
    private static Level _level;

    [Before(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Setup()
    {
        _doc = Application.NewProjectDocument(UnitSystem.Imperial);

        using var tx = new Transaction(_doc, "Setup Finish Schedule Test");
        tx.Start();

        _level = Level.Create(_doc, 0.0);
        _level.Name = "Finish Test Level";

        var room = _doc.Create.NewRoom(_level, new UV(5, 5));
        room.Name = "Test Room";
        room.Number = "101";
        room.get_Parameter(BuiltInParameter.ROOM_FINISH_FLOOR)?.Set("Tile");
        room.get_Parameter(BuiltInParameter.ROOM_FINISH_WALL)?.Set("Paint");
        room.get_Parameter(BuiltInParameter.ROOM_FINISH_CEILING)?.Set("Gypsum");

        tx.Commit();
    }

    [After(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Cleanup()
    {
        _doc?.Close(false);
    }

    [Test]
    public async Task CreateFinishSchedule_RoomsCategory_ScheduleIsCreated()
    {
        ViewSchedule schedule = null;

        using (var tx = new Transaction(_doc, "Create Finish Schedule"))
        {
            tx.Start();
            schedule = ViewSchedule.CreateSchedule(_doc, new ElementId(BuiltInCategory.OST_Rooms));
            schedule.Name = "MCP Room Finish Schedule Test";

            var definition = schedule.Definition;
            var fields = new[] { "Number", "Name", "Floor Finish", "Wall Finish", "Ceiling Finish" };
            foreach (var fieldName in fields)
            {
                foreach (var schedulableField in definition.GetSchedulableFields())
                {
                    if (!schedulableField.GetName(_doc).Equals(fieldName, StringComparison.OrdinalIgnoreCase))
                        continue;

                    definition.AddField(schedulableField);
                    break;
                }
            }

            tx.Commit();
        }

        await Assert.That(schedule).IsNotNull();
        await Assert.That(schedule.Definition.GetFieldCount()).IsGreaterThanOrEqualTo(3);
    }

    [Test]
    public async Task CreateFinishSchedule_RoomHasFinishParameters_ValuesPersist()
    {
        var room = new FilteredElementCollector(_doc)
            .OfCategory(BuiltInCategory.OST_Rooms)
            .WhereElementIsNotElementType()
            .Cast<Room>()
            .First();

        await Assert.That(room.get_Parameter(BuiltInParameter.ROOM_FINISH_FLOOR)?.AsString())
            .IsEqualTo("Tile");
        await Assert.That(room.get_Parameter(BuiltInParameter.ROOM_FINISH_WALL)?.AsString())
            .IsEqualTo("Paint");
        await Assert.That(room.get_Parameter(BuiltInParameter.ROOM_FINISH_CEILING)?.AsString())
            .IsEqualTo("Gypsum");
    }
}
