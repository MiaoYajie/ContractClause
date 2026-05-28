using System.Text.Json.Serialization;

namespace ContractClause.Infrastructure.External.Fatianshi.Models;

internal sealed class FindUpdatedTemplatesResponse
{
    [JsonPropertyName("Data")]
    public List<FatianshiTemplateItemDto> Data { get; set; } = [];

    [JsonPropertyName("Total")]
    public int Total { get; set; }
}

internal sealed class FatianshiTemplateItemDto
{
    [JsonPropertyName("Id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("Number")]
    public int? Number { get; set; }

    [JsonPropertyName("Title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("Title2")]
    public string? Title2 { get; set; }

    [JsonPropertyName("Brief")]
    public string? Brief { get; set; }

    [JsonPropertyName("TemplateTypeName")]
    public string? TemplateTypeName { get; set; }

    [JsonPropertyName("TemplateCategoryNames")]
    public List<string> TemplateCategoryNames { get; set; } = [];

    [JsonPropertyName("ContentTag")]
    public List<string> ContentTag { get; set; } = [];

    [JsonPropertyName("VersionTag")]
    public List<string> VersionTag { get; set; } = [];

    [JsonPropertyName("PartyTag")]
    public List<string> PartyTag { get; set; } = [];

    [JsonPropertyName("SceneFor")]
    public List<string> SceneFor { get; set; } = [];

    [JsonPropertyName("PublishedOn")]
    public DateTimeOffset? PublishedOn { get; set; }

    [JsonPropertyName("Version")]
    public int Version { get; set; }
}
