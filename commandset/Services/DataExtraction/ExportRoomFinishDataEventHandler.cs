using System.Diagnostics;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using RevitMCPCommandSet.Models.DataExtraction;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services.DataExtraction
{
    public class ExportRoomFinishDataEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private bool _includeUnplacedRooms;
        private bool _includeNotEnclosedRooms;
        private bool _includeMaterials = true;

        public ExportRoomFinishDataResult ResultInfo { get; private set; }
        public bool TaskCompleted { get; private set; }
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public void SetParameters(
            bool includeUnplacedRooms = false,
            bool includeNotEnclosedRooms = false,
            bool includeMaterials = true)
        {
            _includeUnplacedRooms = includeUnplacedRooms;
            _includeNotEnclosedRooms = includeNotEnclosedRooms;
            _includeMaterials = includeMaterials;
            TaskCompleted = false;
            _resetEvent.Reset();
        }

        public bool WaitForCompletion(int timeoutMilliseconds = 10000)
        {
            _resetEvent.Reset();
            return _resetEvent.WaitOne(timeoutMilliseconds);
        }

        public void Execute(UIApplication app)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var doc = app.ActiveUIDocument.Document;
                var rooms = new List<RoomFinishDataModel>();
                var globalWarnings = new List<string>();
                int roomsWithMissingFinishes = 0;

                var roomCollector = new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_Rooms)
                    .WhereElementIsNotElementType()
                    .Cast<Room>();

                SpatialElementGeometryCalculator geometryCalculator = null;
                var boundaryOptions = new SpatialElementBoundaryOptions
                {
                    SpatialElementBoundaryLocation = SpatialElementBoundaryLocation.Finish
                };

                foreach (Room room in roomCollector)
                {
                    if (!_includeUnplacedRooms && room.Area == 0)
                        continue;

                    if (!_includeNotEnclosedRooms && room.Area == 0)
                        continue;

                    var roomData = ExtractRoomFinishData(
                        doc, room, boundaryOptions, ref geometryCalculator, _includeMaterials);
                    if (roomData.Warnings.Count > 0)
                        roomsWithMissingFinishes++;

                    rooms.Add(roomData);
                }

                stopwatch.Stop();

                ResultInfo = new ExportRoomFinishDataResult
                {
                    TotalRooms = rooms.Count,
                    RoomsWithMissingFinishes = roomsWithMissingFinishes,
                    Rooms = rooms,
                    Warnings = globalWarnings,
                    ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                    Success = true,
                    Message = $"Successfully exported finish data for {rooms.Count} rooms in {stopwatch.ElapsedMilliseconds} ms"
                };

                if (roomsWithMissingFinishes > 0)
                {
                    ResultInfo.Warnings.Add(
                        $"{roomsWithMissingFinishes} room(s) have missing finish parameter(s). See per-room warnings.");
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                ResultInfo = new ExportRoomFinishDataResult
                {
                    ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                    Success = false,
                    Message = $"Error exporting room finish data: {ex.Message}"
                };
            }
            finally
            {
                TaskCompleted = true;
                _resetEvent.Set();
            }
        }

        private static RoomFinishDataModel ExtractRoomFinishData(
            Document doc,
            Room room,
            SpatialElementBoundaryOptions boundaryOptions,
            ref SpatialElementGeometryCalculator geometryCalculator,
            bool includeMaterials)
        {
            var roomData = new RoomFinishDataModel
            {
#if REVIT2024_OR_GREATER
                RoomId = room.Id.Value,
#else
                RoomId = room.Id.IntegerValue,
#endif
                UniqueId = room.UniqueId,
                Name = room.get_Parameter(BuiltInParameter.ROOM_NAME)?.AsString() ?? "",
                Number = room.Number ?? "",
                Level = room.Level?.Name ?? "No Level",
                Area = GetRoomAreaSquareMeters(room)
            };

            roomData.FloorFinish = GetFinishParameter(
                room, BuiltInParameter.ROOM_FINISH_FLOOR, "floor", roomData.Warnings);
            roomData.WallFinish = GetFinishParameter(
                room, BuiltInParameter.ROOM_FINISH_WALL, "wall", roomData.Warnings);
            roomData.CeilingFinish = GetFinishParameter(
                room, BuiltInParameter.ROOM_FINISH_CEILING, "ceiling", roomData.Warnings);

            if (includeMaterials && room.Area > 0)
            {
                roomData.Materials = ExtractFaceMaterials(doc, room, boundaryOptions, ref geometryCalculator);
            }

            return roomData;
        }

        private static string GetFinishParameter(
            Room room,
            BuiltInParameter parameter,
            string finishLabel,
            List<string> warnings)
        {
            var value = room.get_Parameter(parameter)?.AsString();
            if (string.IsNullOrWhiteSpace(value))
            {
                warnings.Add($"Room {room.Number} ({room.Id}): {finishLabel} finish is not set");
                return null;
            }

            return value.Trim();
        }

        private static List<RoomFinishMaterialModel> ExtractFaceMaterials(
            Document doc,
            Room room,
            SpatialElementBoundaryOptions boundaryOptions,
            ref SpatialElementGeometryCalculator geometryCalculator)
        {
            var materialMap = new Dictionary<string, RoomFinishMaterialModel>();

            try
            {
                if (geometryCalculator == null)
                    geometryCalculator = new SpatialElementGeometryCalculator(doc, boundaryOptions);

                if (!SpatialElementGeometryCalculator.CanCalculateGeometry(room))
                    return new List<RoomFinishMaterialModel>();

                var results = geometryCalculator.CalculateSpatialElementGeometry(room);
                var solid = results.GetGeometry();
                if (solid == null)
                    return new List<RoomFinishMaterialModel>();

                foreach (Face face in solid.Faces)
                {
                    var materialId = face.MaterialElementId;
                    if (materialId == null || materialId == ElementId.InvalidElementId)
                        continue;

                    var material = doc.GetElement(materialId) as Material;
                    if (material == null)
                        continue;

                    string surfaceType = ClassifySurfaceType(face);
                    string key = $"{surfaceType}|{materialId}";

                    if (!materialMap.TryGetValue(key, out var entry))
                    {
                        entry = new RoomFinishMaterialModel
                        {
                            SurfaceType = surfaceType,
#if REVIT2024_OR_GREATER
                            MaterialId = materialId.Value,
#else
                            MaterialId = materialId.IntegerValue,
#endif
                            MaterialName = material.Name,
                            Area = 0
                        };
                        materialMap[key] = entry;
                    }

                    entry.Area += ToSquareMeters(face.Area);
                }
            }
            catch
            {
                // Geometry may be unavailable for unenclosed or invalid rooms.
            }

            return materialMap.Values
                .Where(m => m.Area > 0)
                .OrderBy(m => m.SurfaceType)
                .ThenBy(m => m.MaterialName)
                .ToList();
        }

        private static string ClassifySurfaceType(Face face)
        {
            var normal = face.ComputeNormal(new UV(0.5, 0.5));
            if (normal.Z > 0.7)
                return "floor";
            if (normal.Z < -0.7)
                return "ceiling";
            return "wall";
        }

        private static double? GetRoomAreaSquareMeters(Room room)
        {
            var areaParam = room.get_Parameter(BuiltInParameter.ROOM_AREA);
            if (areaParam == null || !areaParam.HasValue)
                return null;

            double internalArea = areaParam.AsDouble();
            if (internalArea <= 0)
                return null;

            return ToSquareMeters(internalArea);
        }

        private static double ToSquareMeters(double internalSquareFeet)
        {
#if REVIT2022_OR_GREATER
            return UnitUtils.ConvertFromInternalUnits(internalSquareFeet, UnitTypeId.SquareMeters);
#else
            return UnitUtils.ConvertFromInternalUnits(internalSquareFeet, DisplayUnitType.DUT_SQUARE_METERS);
#endif
        }

        public string GetName()
        {
            return "Export Room Finish Data";
        }
    }
}
