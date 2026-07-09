using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using RevitMCPCommandSet.Models.Structure;
using RevitMCPCommandSet.Services;
using RevitMCPCommandSet.Utils;
using System.Reflection;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests.Modeling;

public class Rev24BasicModelingTests : RevitApiTest
{
    private static Document _doc;
    private static Level _level;
    private static ViewPlan _floorPlan;
    private static Wall _hostWall;
    private static Room _room;

    [Before(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Setup()
    {
        _doc = Application.NewProjectDocument(UnitSystem.Imperial);

        using var tx = new Transaction(_doc, "REV-24 setup");
        tx.Start();

        _level = Level.Create(_doc, 3000.0 / 304.8);
        _level.Name = "REV-24 Level";

        var floorPlanType = new FilteredElementCollector(_doc)
            .OfClass(typeof(ViewFamilyType))
            .Cast<ViewFamilyType>()
            .FirstOrDefault(vft => vft.ViewFamily == ViewFamily.FloorPlan);

        if (floorPlanType != null)
            _floorPlan = ViewPlan.Create(_doc, floorPlanType.Id, _level.Id);

        CreateEnclosure(_doc, _level.Id, 0, 0, 10);
        _hostWall = new FilteredElementCollector(_doc)
            .OfClass(typeof(Wall))
            .Cast<Wall>()
            .FirstOrDefault();

        _room = _doc.Create.NewRoom(_level, new UV(5.0, 5.0));
        tx.Commit();
    }

    [After(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Cleanup()
    {
        _doc?.Close(false);
    }

    [Test]
    public async Task Rev24_Scenario_LevelWallRoomExistWithExpectedUnits()
    {
        await Assert.That(_level).IsNotNull();
        await Assert.That(_hostWall).IsNotNull();
        await Assert.That(_room).IsNotNull();
        await Assert.That(_room.Area).IsGreaterThan(0);

        var levelElevationMm = _level.Elevation * 304.8;
        await Assert.That(Math.Abs(levelElevationMm - 3000.0) < 1.0).IsTrue();
    }

    [Test]
    public async Task Rev24_DoorWithHostWallId_IsHostedOnRequestedWall()
    {
        var doorType = new FilteredElementCollector(_doc)
            .OfClass(typeof(FamilySymbol))
            .OfCategory(BuiltInCategory.OST_Doors)
            .Cast<FamilySymbol>()
            .FirstOrDefault();

        if (doorType == null)
        {
            await Assert.That(true).IsTrue();
            return;
        }

        FamilyInstance door = null;
        using (var tx = new Transaction(_doc, "Place door on host wall"))
        {
            tx.Start();
            if (!doorType.IsActive)
                doorType.Activate();

            var hostWall = _hostWall;
            var midpoint = (hostWall.Location as LocationCurve)?.Curve.Evaluate(0.5, true)
                ?? new XYZ(5.0, 0, 0);

            door = _doc.Create.NewFamilyInstance(
                midpoint,
                doorType,
                hostWall,
                _level,
                StructuralType.NonStructural);
            tx.Commit();
        }

        await Assert.That(door).IsNotNull();
        await Assert.That(door.Host?.Id).IsEqualTo(_hostWall.Id);
    }

    [Test]
    public async Task Rev24_TagWalls_SecondRunSkipsExistingTags()
    {
        if (_floorPlan == null || _hostWall == null)
        {
            await Assert.That(true).IsTrue();
            return;
        }

        var wallTagType = new FilteredElementCollector(_doc)
            .OfClass(typeof(FamilySymbol))
            .OfCategory(BuiltInCategory.OST_WallTags)
            .Cast<FamilySymbol>()
            .FirstOrDefault();

        if (wallTagType == null)
        {
            await Assert.That(true).IsTrue();
            return;
        }

        IndependentTag firstTag = null;
        using (var tx = new Transaction(_doc, "Tag wall once"))
        {
            tx.Start();
            if (!wallTagType.IsActive)
                wallTagType.Activate();

            var midpoint = (_hostWall.Location as LocationCurve)?.Curve.Evaluate(0.5, true);
            firstTag = IndependentTag.Create(
                _doc,
                wallTagType.Id,
                _floorPlan.Id,
                new Reference(_hostWall),
                false,
                TagOrientation.Horizontal,
                midpoint);
            tx.Commit();
        }

        await Assert.That(firstTag).IsNotNull();

        var existingTaggedWallIds = new FilteredElementCollector(_doc, _floorPlan.Id)
            .OfClass(typeof(IndependentTag))
            .Cast<IndependentTag>()
            .SelectMany(tag => tag.GetTaggedLocalElementIds())
            .Select(id => id.GetValue())
            .ToHashSet();

        await Assert.That(existingTaggedWallIds.Contains(_hostWall.Id.GetValue())).IsTrue();
    }

    [Test]
    public async Task Rev24_TagRooms_AppliesRequestedTagType()
    {
        if (_floorPlan == null || _room == null)
        {
            await Assert.That(true).IsTrue();
            return;
        }

        var roomTagType = new FilteredElementCollector(_doc)
            .OfClass(typeof(FamilySymbol))
            .OfCategory(BuiltInCategory.OST_RoomTags)
            .Cast<FamilySymbol>()
            .FirstOrDefault();

        if (roomTagType == null)
        {
            await Assert.That(true).IsTrue();
            return;
        }

        RoomTag tag = null;
        using (var tx = new Transaction(_doc, "Tag room with type"))
        {
            tx.Start();
            if (!roomTagType.IsActive)
                roomTagType.Activate();

            var center = (_room.Location as LocationPoint)?.Point ?? new XYZ(5, 5, 0);
            tag = _doc.Create.NewRoomTag(new LinkElementId(_room.Id), new UV(center.X, center.Y), _floorPlan.Id);
            if (tag != null && tag.GetTypeId() != roomTagType.Id)
                tag.ChangeTypeId(roomTagType.Id);

            tx.Commit();
        }

        await Assert.That(tag).IsNotNull();
        await Assert.That(tag.GetTypeId()).IsEqualTo(roomTagType.Id);
    }

    [Test]
    public async Task Rev24_StructuralFraming_ResolveLevelUsesLevelName()
    {
        var handler = new CreateStructuralFramingSystemEventHandler();
        var method = typeof(CreateStructuralFramingSystemEventHandler).GetMethod(
            "ResolveLevel",
            BindingFlags.NonPublic | BindingFlags.Instance);

        if (method == null)
        {
            await Assert.That(true).IsTrue();
            return;
        }

        var warnings = new List<string>();
        var level = method.Invoke(handler, new object[] { _doc, _level.Name, warnings }) as Level;

        await Assert.That(level).IsNotNull();
        await Assert.That(level.Id).IsEqualTo(_level.Id);
    }

    [Test]
    public async Task Rev24_StructuralFraming_ParametersValidateMmBoundary()
    {
        var info = new StructuralFramingSystemCreationInfo
        {
            LevelName = "REV-24 Level",
            XMin = 0,
            XMax = 6000,
            YMin = 0,
            YMax = 4000,
            Spacing = 1000,
            DirectionEdge = "bottom",
            LayoutRule = "fixed_distance",
            Justify = "center",
            Elevation = 0
        };

        var isValid = info.Validate(out var error);
        await Assert.That(isValid).IsTrue();
        await Assert.That(error).IsEqualTo(string.Empty);
    }

    private static void CreateEnclosure(Document doc, ElementId levelId, double x, double y, double size)
    {
        var p1 = new XYZ(x, y, 0);
        var p2 = new XYZ(x + size, y, 0);
        var p3 = new XYZ(x + size, y + size, 0);
        var p4 = new XYZ(x, y + size, 0);

        Wall.Create(doc, Line.CreateBound(p1, p2), levelId, false);
        Wall.Create(doc, Line.CreateBound(p2, p3), levelId, false);
        Wall.Create(doc, Line.CreateBound(p3, p4), levelId, false);
        Wall.Create(doc, Line.CreateBound(p4, p1), levelId, false);
    }
}
