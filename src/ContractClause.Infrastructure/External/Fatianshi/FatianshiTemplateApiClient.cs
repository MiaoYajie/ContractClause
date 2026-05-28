using System.Net.Http.Json;
using System.Text.Json;
using ContractClause.Application.Common.Interfaces;
using ContractClause.Application.Options;
using ContractClause.Infrastructure.External.Fatianshi.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ContractClause.Infrastructure.External.Fatianshi;

public class FatianshiTemplateApiClient(
    HttpClient http,
    IOptions<FatianshiTemplateSyncOptions> options,
    ILogger<FatianshiTemplateApiClient> logger) : IFatianshiTemplateApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly FatianshiTemplateSyncOptions _options = options.Value;

    public async Task<FatianshiTemplatePage> FetchUpdatedTemplatesPageAsync(int page, CancellationToken ct = default)
    {
        var size = _options.PageSize > 0 ? _options.PageSize : 50;
        var query = $"size={size}&page={page}";
        if (_options.RecommendOnly)
            query += "&recommend=true";

        var path = $"/content/ForReview/FindUpdatedTemplates?{query}";

        using var response = await http.GetAsync(path, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("法天使 FindUpdatedTemplates 失败: {Status} {Body}", response.StatusCode, Truncate(body));
            response.EnsureSuccessStatusCode();
        }

        var payload = JsonSerializer.Deserialize<FindUpdatedTemplatesResponse>(body, JsonOptions)
            ?? new FindUpdatedTemplatesResponse();

        var items = payload.Data
            .Select(MapMetadata)
            .Where(x => x is not null)
            .Cast<FatianshiTemplateMetadata>()
            .ToList();

        return new FatianshiTemplatePage(items, payload.Total);
    }

    private static FatianshiTemplateMetadata? MapMetadata(FatianshiTemplateItemDto dto)
    {
        if (!Guid.TryParse(dto.Id, out var id))
            return null;

        if (!dto.PublishedOn.HasValue)
            return null;

        var tags = dto.ContentTag
            .Concat(dto.VersionTag)
            .Concat(dto.PartyTag)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct()
            .ToList();

        var scenarios = string.Join(' ',
            dto.SceneFor.Where(s => !string.IsNullOrWhiteSpace(s)));

        return new FatianshiTemplateMetadata(
            id,
            dto.Number,
            dto.Title,
            dto.Title2 ?? string.Empty,
            dto.TemplateTypeName ?? string.Empty,
            dto.TemplateCategoryNames,
            tags,
            dto.Brief ?? string.Empty,
            scenarios,
            dto.PublishedOn.Value.UtcDateTime,
            dto.Version);
    }

    private static string Truncate(string s) => s.Length <= 500 ? s : s[..500] + "...";
}
