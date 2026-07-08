using Newtonsoft.Json;

namespace RevitMCPCommandSet.Models.DataExtraction
{
    public class TepUnits
    {
        [JsonProperty("length")]
        public string Length { get; set; } = "mm";

        [JsonProperty("area")]
        public string Area { get; set; } = "m2";

        [JsonProperty("volume")]
        public string Volume { get; set; } = "m3";
    }

    public class TepLevelData
    {
        [JsonProperty("levelName")]
        public string LevelName { get; set; }

        [JsonProperty("elevation")]
        public double Elevation { get; set; }

        [JsonProperty("area")]
        public double Area { get; set; }

        [JsonProperty("volume")]
        public double Volume { get; set; }

        [JsonProperty("roomCount")]
        public int RoomCount { get; set; }
    }

    public class TepRoomPurposeData
    {
        [JsonProperty("purpose")]
        public string Purpose { get; set; }

        [JsonProperty("roomCount")]
        public int RoomCount { get; set; }

        [JsonProperty("area")]
        public double Area { get; set; }

        [JsonProperty("volume")]
        public double Volume { get; set; }
    }

    public class ExportTepDataResult
    {
        [JsonProperty("projectName")]
        public string ProjectName { get; set; }

        [JsonProperty("units")]
        public TepUnits Units { get; set; } = new TepUnits();

        [JsonProperty("buildingFootprintArea")]
        public double BuildingFootprintArea { get; set; }

        [JsonProperty("totalArea")]
        public double TotalArea { get; set; }

        [JsonProperty("totalVolume")]
        public double TotalVolume { get; set; }

        [JsonProperty("storeyCount")]
        public int StoreyCount { get; set; }

        [JsonProperty("totalRooms")]
        public int TotalRooms { get; set; }

        [JsonProperty("levels")]
        public List<TepLevelData> Levels { get; set; } = new List<TepLevelData>();

        [JsonProperty("roomsByPurpose")]
        public List<TepRoomPurposeData> RoomsByPurpose { get; set; } = new List<TepRoomPurposeData>();

        [JsonProperty("executionTimeMs")]
        public long ExecutionTimeMs { get; set; }

        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }
}
