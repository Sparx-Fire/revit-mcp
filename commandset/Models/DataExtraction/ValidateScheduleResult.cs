using Newtonsoft.Json;

namespace RevitMCPCommandSet.Models.DataExtraction
{
    public class ValidateScheduleResult
    {
        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("scheduleName")]
        public string ScheduleName { get; set; }

        [JsonProperty("levelName")]
        public string LevelName { get; set; }

        [JsonProperty("modelCount")]
        public int ModelCount { get; set; }

        [JsonProperty("scheduleCount")]
        public int ScheduleCount { get; set; }

        [JsonProperty("diff")]
        public int Diff { get; set; }

        [JsonProperty("missingIds")]
        public List<long> MissingIds { get; set; } = new List<long>();

        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }
}
