using Newtonsoft.Json;

namespace RevitMCPCommandSet.Models.Common;

public class DocumentStylesResult
{
    [JsonProperty("success")]
    public bool Success { get; set; }

    [JsonProperty("message")]
    public string Message { get; set; } = string.Empty;

    [JsonProperty("dimensionTypes")]
    public List<DimensionTypeStyleInfo> DimensionTypes { get; set; } = new();

    [JsonProperty("gridTypes")]
    public List<NamedStyleInfo> GridTypes { get; set; } = new();

    [JsonProperty("textNoteTypes")]
    public List<TextNoteTypeStyleInfo> TextNoteTypes { get; set; } = new();

    [JsonProperty("linePatterns")]
    public List<NamedStyleInfo> LinePatterns { get; set; } = new();

    [JsonProperty("graphicsStyles")]
    public List<GraphicsStyleInfo> GraphicsStyles { get; set; } = new();

    [JsonProperty("titleBlocks")]
    public List<TitleBlockStyleInfo> TitleBlocks { get; set; } = new();
}

public class NamedStyleInfo
{
    [JsonProperty("id")]
    public long Id { get; set; }

    [JsonProperty("uniqueId")]
    public string UniqueId { get; set; } = string.Empty;

    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;
}

public class DimensionTypeStyleInfo : NamedStyleInfo
{
    [JsonProperty("styleType")]
    public string StyleType { get; set; } = string.Empty;
}

public class TextNoteTypeStyleInfo : NamedStyleInfo
{
    [JsonProperty("textHeightMm")]
    public double? TextHeightMm { get; set; }

    [JsonProperty("font")]
    public string Font { get; set; } = string.Empty;
}

public class GraphicsStyleInfo : NamedStyleInfo
{
    [JsonProperty("graphicsStyleType")]
    public string GraphicsStyleType { get; set; } = string.Empty;

    [JsonProperty("category")]
    public string Category { get; set; } = string.Empty;
}

public class TitleBlockStyleInfo
{
    [JsonProperty("id")]
    public long Id { get; set; }

    [JsonProperty("uniqueId")]
    public string UniqueId { get; set; } = string.Empty;

    [JsonProperty("familyName")]
    public string FamilyName { get; set; } = string.Empty;

    [JsonProperty("typeName")]
    public string TypeName { get; set; } = string.Empty;
}
