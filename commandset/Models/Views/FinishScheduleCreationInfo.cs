using Newtonsoft.Json;

namespace RevitMCPCommandSet.Models.Views;

public class FinishScheduleCreationInfo
{
    [JsonProperty("name")]
    public string Name { get; set; } = "Room Finish Schedule";

    [JsonProperty("templateId")]
    public string TemplateId { get; set; } = string.Empty;

    [JsonProperty("type")]
    public string Type { get; set; } = "Regular";

    [JsonProperty("includeUnplacedRooms")]
    public bool IncludeUnplacedRooms { get; set; }

    [JsonProperty("includeNotEnclosedRooms")]
    public bool IncludeNotEnclosedRooms { get; set; }

    [JsonProperty("missingFinishWarningThreshold")]
    public double MissingFinishWarningThreshold { get; set; } = 0.30;
}

public class FinishScheduleCreationResult
{
    [JsonProperty("success")]
    public bool Success { get; set; }

    [JsonProperty("message")]
    public string Message { get; set; } = string.Empty;

    [JsonProperty("scheduleId")]
    public long ScheduleId { get; set; }

    [JsonProperty("scheduleUniqueId")]
    public string ScheduleUniqueId { get; set; } = string.Empty;

    [JsonProperty("scheduleName")]
    public string ScheduleName { get; set; } = string.Empty;

    [JsonProperty("templateId")]
    public string TemplateId { get; set; } = string.Empty;

    [JsonProperty("totalRooms")]
    public int TotalRooms { get; set; }

    [JsonProperty("roomsWithMissingFinishes")]
    public int RoomsWithMissingFinishes { get; set; }

    [JsonProperty("missingFinishRatio")]
    public double MissingFinishRatio { get; set; }

    [JsonProperty("fieldCount")]
    public int FieldCount { get; set; }

    [JsonProperty("executionTimeMs")]
    public long ExecutionTimeMs { get; set; }

    [JsonProperty("warnings")]
    public List<string> Warnings { get; set; } = new List<string>();
}
