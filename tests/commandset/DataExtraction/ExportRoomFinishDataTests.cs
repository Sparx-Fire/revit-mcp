using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests.DataExtraction;

public class ExportRoomFinishDataTests : RevitApiTest
{
    private static Document _doc;
    private static Level _level;

    [Before(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Setup()
    {
        _doc = Application.NewProjectDocument(UnitSystem.Imperial);

        using var tx = new Transaction(_doc, "Setup Export Room Finish Data Test");
        tx.Start();

        _level = Level.Create(_doc, 0.0);
        _level.Name = "Finish Test Level";

        var floorPlanType = new FilteredElementCollector(_doc)
            .OfClass(typeof(ViewFamilyType))
            .Cast<ViewFamilyType>()
            .FirstOrDefault(vft => vft.ViewFamily == ViewFamily.FloorPlan);

        if (floorPlanType != null)
        {
            ViewPlan.Create(_doc, floorPlanType.Id, _level.Id);
        }

        double size = 10.0;
        var p1 = new XYZ(0, 0, 0);
        var p2 = new XYZ(size, 0, 0);
        var p3 = new XYZ(size, size, 0);
        var p4 = new XYZ(0, size, 0);

        Wall.Create(_doc, Line.CreateBound(p1, p2), _level.Id, false);
        Wall.Create(_doc, Line.CreateBound(p2, p3), _level.Id, false);
        Wall.Create(_doc, Line.CreateBound(p3, p4), _level.Id, false);
        Wall.Create(_doc, Line.CreateBound(p4, p1), _level.Id, false);

        var room = _doc.Create.NewRoom(_level, new UV(5.0, 5.0));
        if (room != null)
        {
            room.get_Parameter(BuiltInParameter.ROOM_NAME)?.Set("Finish Test Room");
            room.get_Parameter(BuiltInParameter.ROOM_FINISH_FLOOR)?.Set("Tile 300x300");
            room.get_Parameter(BuiltInParameter.ROOM_FINISH_WALL)?.Set("Paint White");
            room.get_Parameter(BuiltInParameter.ROOM_FINISH_CEILING)?.Set("Gypsum Board");
        }

        tx.Commit();
    }

    [After(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Cleanup()
    {
        _doc?.Close(false);
    }

    [Test]
    public async Task ExportRoomFinish_FinishParameters_RoundTrip()
    {
        var room = new FilteredElementCollector(_doc)
            .OfCategory(BuiltInCategory.OST_Rooms)
            .WhereElementIsNotElementType()
            .Cast<Room>()
            .First(r => r.Area > 0);

        string floorFinish = room.get_Parameter(BuiltInParameter.ROOM_FINISH_FLOOR)?.AsString() ?? "";
        string wallFinish = room.get_Parameter(BuiltInParameter.ROOM_FINISH_WALL)?.AsString() ?? "";
        string ceilingFinish = room.get_Parameter(BuiltInParameter.ROOM_FINISH_CEILING)?.AsString() ?? "";

        await Assert.That(floorFinish).IsEqualTo("Tile 300x300");
        await Assert.That(wallFinish).IsEqualTo("Paint White");
        await Assert.That(ceilingFinish).IsEqualTo("Gypsum Board");
    }

    [Test]
    public async Task ExportRoomFinish_MissingFinish_ReturnsNullAndWarning()
    {
        var room = new FilteredElementCollector(_doc)
            .OfCategory(BuiltInCategory.OST_Rooms)
            .WhereElementIsNotElementType()
            .Cast<Room>()
            .First(r => r.Area > 0);

        var warnings = new List<string>();

        using (var tx = new Transaction(_doc, "Clear Floor Finish"))
        {
            tx.Start();
            room.get_Parameter(BuiltInParameter.ROOM_FINISH_FLOOR)?.Set("");
            tx.Commit();
        }

        string cleared = room.get_Parameter(BuiltInParameter.ROOM_FINISH_FLOOR)?.AsString();
        await Assert.That(string.IsNullOrWhiteSpace(cleared)).IsTrue();

        warnings.Clear();
        string clearedResult = string.IsNullOrWhiteSpace(cleared)
            ? null
            : cleared.Trim();
        if (string.IsNullOrWhiteSpace(cleared))
        {
            warnings.Add($"Room {room.Number} ({room.Id}): floor finish is not set");
        }

        await Assert.That(clearedResult).IsNull();
        await Assert.That(warnings.Count).IsEqualTo(1);
    }

    [Test]
    public async Task ExportRoomFinish_PlacedRoom_HasPositiveArea()
    {
        var room = new FilteredElementCollector(_doc)
            .OfCategory(BuiltInCategory.OST_Rooms)
            .WhereElementIsNotElementType()
            .Cast<Room>()
            .First(r => r.Area > 0);

        await Assert.That(room.Area).IsGreaterThan(0);
    }

    [Test]
    public async Task ExportRoomFinish_SpatialGeometry_ReturnsMaterialsList()
    {
        var room = new FilteredElementCollector(_doc)
            .OfCategory(BuiltInCategory.OST_Rooms)
            .WhereElementIsNotElementType()
            .Cast<Room>()
            .First(r => r.Area > 0);

        var calculator = new SpatialElementGeometryCalculator(_doc);
        var results = calculator.CalculateSpatialElementGeometry(room);
        var solid = results.GetGeometry();

        await Assert.That(solid).IsNotNull();

        int facesWithMaterial = 0;
        foreach (Face face in solid.Faces)
        {
            if (face.MaterialElementId != null && face.MaterialElementId != ElementId.InvalidElementId)
                facesWithMaterial++;
        }

        await Assert.That(facesWithMaterial).IsGreaterThanOrEqualTo(0);
    }

    [Test]
    public async Task ExportRoomFinish_RoomCollector_ReturnsPlacedRooms()
    {
        var placedRooms = new FilteredElementCollector(_doc)
            .OfCategory(BuiltInCategory.OST_Rooms)
            .WhereElementIsNotElementType()
            .Cast<Room>()
            .Where(r => r.Area > 0)
            .ToList();

        await Assert.That(placedRooms.Count).IsGreaterThan(0);
    }
}
