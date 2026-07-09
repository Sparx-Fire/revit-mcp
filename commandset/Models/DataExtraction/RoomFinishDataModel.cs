using Newtonsoft.Json;

namespace RevitMCPCommandSet.Models.DataExtraction
{
    public class RoomFinishMaterialModel
    {
        [JsonProperty("surfaceType")]
        public string SurfaceType { get; set; }

        [JsonProperty("materialId")]
        public long? MaterialId { get; set; }

        [JsonProperty("materialName")]
        public string MaterialName { get; set; }

        [JsonProperty("area")]
        public double Area { get; set; }
    }

    public class RoomFinishDataModel
    {
        [JsonProperty("roomId")]
        public long RoomId { get; set; }

        [JsonProperty("uniqueId")]
        public string UniqueId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("number")]
        public string Number { get; set; }

        [JsonProperty("level")]
        public string Level { get; set; }

        [JsonProperty("area")]
        public double? Area { get; set; }

        [JsonProperty("floorFinish")]
        public string FloorFinish { get; set; }

        [JsonProperty("wallFinish")]
        public string WallFinish { get; set; }

        [JsonProperty("ceilingFinish")]
        public string CeilingFinish { get; set; }

        [JsonProperty("materials")]
        public List<RoomFinishMaterialModel> Materials { get; set; } = new List<RoomFinishMaterialModel>();

        [JsonProperty("warnings")]
        public List<string> Warnings { get; set; } = new List<string>();
    }

    public class ExportRoomFinishDataResult
    {
        [JsonProperty("totalRooms")]
        public int TotalRooms { get; set; }

        [JsonProperty("roomsWithMissingFinishes")]
        public int RoomsWithMissingFinishes { get; set; }

        [JsonProperty("rooms")]
        public List<RoomFinishDataModel> Rooms { get; set; } = new List<RoomFinishDataModel>();

        [JsonProperty("warnings")]
        public List<string> Warnings { get; set; } = new List<string>();

        [JsonProperty("executionTimeMs")]
        public long ExecutionTimeMs { get; set; }

        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }
}
