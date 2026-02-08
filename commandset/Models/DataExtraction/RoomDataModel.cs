using Newtonsoft.Json;

namespace RevitMCPCommandSet.Models.DataExtraction
{
    /// <summary>
    /// Model for room data extraction
    /// </summary>
    public class RoomDataModel
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("uniqueId")]
        public string UniqueId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("number")]
        public string Number { get; set; }

        [JsonProperty("level")]
        public string Level { get; set; }

        [JsonProperty("area")]
        public double Area { get; set; } // Square feet

        [JsonProperty("volume")]
        public double Volume { get; set; } // Cubic feet

        [JsonProperty("perimeter")]
        public double Perimeter { get; set; } // Feet

        [JsonProperty("unboundedHeight")]
        public double UnboundedHeight { get; set; } // Feet

        [JsonProperty("department")]
        public string Department { get; set; }

        [JsonProperty("comments")]
        public string Comments { get; set; }

        [JsonProperty("phase")]
        public string Phase { get; set; }

        [JsonProperty("occupancy")]
        public string Occupancy { get; set; }
    }

    /// <summary>
    /// Result container for room data export
    /// </summary>
    public class ExportRoomDataResult
    {
        [JsonProperty("totalRooms")]
        public int TotalRooms { get; set; }

        [JsonProperty("totalArea")]
        public double TotalArea { get; set; }

        [JsonProperty("rooms")]
        public List<RoomDataModel> Rooms { get; set; } = new List<RoomDataModel>();

        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }
}
