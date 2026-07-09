using System.Diagnostics;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using RevitMCPCommandSet.Services.DataExtraction;
using RevitMCPCommandSet.Utils;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests.DataExtraction;

public class ExportTepDataTests : RevitApiTest
{
    private static Document _doc;
    private static Level _level1;
    private static Level _level2;

    [Before(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Setup()
    {
        _doc = Application.NewProjectDocument(UnitSystem.Imperial);

        using var tx = new Transaction(_doc, "Setup Export TEP Data Test");
        tx.Start();

        _level1 = Level.Create(_doc, 0.0);
        _level1.Name = "TEP Level 1";

        _level2 = Level.Create(_doc, 4000.0 / 304.8);
        _level2.Name = "TEP Level 2";

        var floorPlanType = new FilteredElementCollector(_doc)
            .OfClass(typeof(ViewFamilyType))
            .Cast<ViewFamilyType>()
            .FirstOrDefault(vft => vft.ViewFamily == ViewFamily.FloorPlan);

        if (floorPlanType != null)
        {
            ViewPlan.Create(_doc, floorPlanType.Id, _level1.Id);
            ViewPlan.Create(_doc, floorPlanType.Id, _level2.Id);
        }

        CreateEnclosedRoom(_doc, _level1, 10.0, new UV(5.0, 5.0), "Living", "Жилые");
        CreateEnclosedRoom(_doc, _level1, 8.0, new UV(20.0, 5.0), "Kitchen", "Жилые");
        CreateEnclosedRoom(_doc, _level2, 10.0, new UV(5.0, 5.0), "Office", "Общественные");

        tx.Commit();
    }

    [After(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Cleanup()
    {
        _doc?.Close(false);
    }

    [Test]
    public async Task ExportTep_TotalArea_MatchesSumOfRoomAreasInSquareMeters()
    {
        var result = ExportTepDataEventHandler.Compute(_doc);

        var rooms = new FilteredElementCollector(_doc)
            .OfCategory(BuiltInCategory.OST_Rooms)
            .WhereElementIsNotElementType()
            .Cast<Room>()
            .Where(room => room.Area > 0)
            .ToList();

        double expectedTotalArea = rooms.Sum(room => RevitUnitConversion.ToSquareMeters(room.Area));

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.TotalArea).IsEqualTo(Math.Round(expectedTotalArea, 2)).Within(0.01);
        await Assert.That(result.TotalRooms).IsEqualTo(rooms.Count);
    }

    [Test]
    public async Task ExportTep_StoreyCount_MatchesLevelsWithPlacedRooms()
    {
        var result = ExportTepDataEventHandler.Compute(_doc);

        await Assert.That(result.StoreyCount).IsEqualTo(2);
        await Assert.That(result.Levels.Count).IsEqualTo(2);
        await Assert.That(result.Levels[0].LevelName).IsEqualTo("TEP Level 1");
        await Assert.That(result.Levels[1].LevelName).IsEqualTo("TEP Level 2");
    }

    [Test]
    public async Task ExportTep_LevelElevations_ConvertedToMillimeters()
    {
        var result = ExportTepDataEventHandler.Compute(_doc);

        double expectedLevel1Elevation = Math.Round(RevitUnitConversion.ToMillimeters(_level1.Elevation), 2);
        double expectedLevel2Elevation = Math.Round(RevitUnitConversion.ToMillimeters(_level2.Elevation), 2);

        await Assert.That(result.Levels[0].Elevation).IsEqualTo(expectedLevel1Elevation).Within(0.01);
        await Assert.That(result.Levels[1].Elevation).IsEqualTo(expectedLevel2Elevation).Within(0.01);
    }

    [Test]
    public async Task ExportTep_RoomsByPurpose_GroupedByDepartment()
    {
        var result = ExportTepDataEventHandler.Compute(_doc);

        var residential = result.RoomsByPurpose.FirstOrDefault(purpose => purpose.Purpose == "Жилые");
        var publicSpaces = result.RoomsByPurpose.FirstOrDefault(purpose => purpose.Purpose == "Общественные");

        await Assert.That(residential).IsNotNull();
        await Assert.That(publicSpaces).IsNotNull();
        await Assert.That(residential!.RoomCount).IsEqualTo(2);
        await Assert.That(publicSpaces!.RoomCount).IsEqualTo(1);
    }

    [Test]
    public async Task ExportTep_BuildingFootprint_UsesLowestLevelRoomAreasOnly()
    {
        var result = ExportTepDataEventHandler.Compute(_doc);

        var level1Rooms = new FilteredElementCollector(_doc)
            .OfCategory(BuiltInCategory.OST_Rooms)
            .WhereElementIsNotElementType()
            .Cast<Room>()
            .Where(room => room.Area > 0 && room.LevelId == _level1.Id)
            .ToList();

        double expectedFootprint = level1Rooms
            .Sum(room => RevitUnitConversion.ToSquareMeters(room.Area));

        await Assert.That(result.BuildingFootprintArea).IsEqualTo(Math.Round(expectedFootprint, 2)).Within(0.01);
    }

    [Test]
    public async Task ExportTep_Units_AreMetric()
    {
        var result = ExportTepDataEventHandler.Compute(_doc);

        await Assert.That(result.Units.Length).IsEqualTo("mm");
        await Assert.That(result.Units.Area).IsEqualTo("m2");
        await Assert.That(result.Units.Volume).IsEqualTo("m3");
        await Assert.That(result.TotalVolume).IsGreaterThan(0);
    }

    [Test]
    public async Task ExportTep_CompletesWithinPerformanceBudget()
    {
        var stopwatch = Stopwatch.StartNew();
        var result = ExportTepDataEventHandler.Compute(_doc);
        stopwatch.Stop();

        await Assert.That(result.Success).IsTrue();
        await Assert.That(stopwatch.ElapsedMilliseconds).IsLessThan(30000);
    }

    private static void CreateEnclosedRoom(
        Document doc,
        Level level,
        double sizeFeet,
        UV location,
        string roomName,
        string department)
    {
        double z = level.Elevation;
        var p1 = new XYZ(location.U, location.V, z);
        var p2 = new XYZ(location.U + sizeFeet, location.V, z);
        var p3 = new XYZ(location.U + sizeFeet, location.V + sizeFeet, z);
        var p4 = new XYZ(location.U, location.V + sizeFeet, z);

        Wall.Create(doc, Line.CreateBound(p1, p2), level.Id, false);
        Wall.Create(doc, Line.CreateBound(p2, p3), level.Id, false);
        Wall.Create(doc, Line.CreateBound(p3, p4), level.Id, false);
        Wall.Create(doc, Line.CreateBound(p4, p1), level.Id, false);

        var room = doc.Create.NewRoom(level, new UV(location.U + sizeFeet / 2.0, location.V + sizeFeet / 2.0));
        if (room == null)
            return;

        room.get_Parameter(BuiltInParameter.ROOM_NAME)?.Set(roomName);
        room.get_Parameter(BuiltInParameter.ROOM_DEPARTMENT)?.Set(department);
    }
}
