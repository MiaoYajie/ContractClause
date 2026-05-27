using System.Text.Json;
using ContractClause.Application.Common.Interfaces;
using ContractClause.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ContractClause.Infrastructure.External.Fatianshi;

public class FatianshiTemplateApiClient(
    HttpClient http,
    IOptions<FatianshiTemplateSyncOptions> options,
    ILogger<FatianshiTemplateApiClient> logger) : IFatianshiTemplateApiClient
{
    private readonly FatianshiTemplateSyncOptions _options = options.Value;

    public async Task<IReadOnlyList<FatianshiTemplateListItem>> SearchUpdatedAsync(
        DateTime? updatedAfter,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = new List<string>();
        if (updatedAfter.HasValue && updatedAfter.Value > DateTime.MinValue)
        {
            query.Add($"{_options.SearchUpdatedAfterParameter}={Uri.EscapeDataString(updatedAfter.Value.ToString("O"))}");
        }
        if (page > 0) query.Add($"page={page}");
        if (pageSize > 0) query.Add($"pageSize={pageSize}");

        var path = "/template/search";
        if (query.Count > 0) path += "?" + string.Join('&', query);

        using var response = await http.GetAsync(path, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("法天使 template/search 失败: {Status} {Body}", response.StatusCode, Truncate(body));
            response.EnsureSuccessStatusCode();
        }

        using var doc = JsonDocument.Parse(body);
        return FatianshiJsonHelper.ExtractListItems(doc)
            .Select(MapListItem)
            .Where(x => !string.IsNullOrWhiteSpace(x.Id))
            .ToList();
    }

    public async Task<FatianshiTemplateDetail> GetTemplateAsync(string externalId, CancellationToken ct = default)
    {
        using var response = await http.GetAsync($"/template/{Uri.EscapeDataString(externalId)}", ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("法天使 template/{{Id}} 失败: {Status} {Body}", response.StatusCode, Truncate(body));
            response.EnsureSuccessStatusCode();
        }

        using var doc = JsonDocument.Parse(body);
        var item = UnwrapData(doc.RootElement);

        var id = FatianshiJsonHelper.GetId(item) ?? externalId;
        var html = FatianshiJsonHelper.GetHtml(item);
        if (string.IsNullOrWhiteSpace(html))
            throw new InvalidOperationException($"模板 {externalId} 响应中未包含 HTML 正文");

        return new FatianshiTemplateDetail(
            id,
            FatianshiJsonHelper.GetTitle(item) ?? id,
            html,
            FatianshiJsonHelper.GetString(item, "type", "templateType", "category"),
            FatianshiJsonHelper.GetStringList(item, "categories", "categoryList", "tags"),
            FatianshiJsonHelper.GetStringList(item, "tags", "tagList"),
            FatianshiJsonHelper.GetUpdatedAt(item));
    }

    private FatianshiTemplateListItem MapListItem(JsonElement el)
    {
        var id = FatianshiJsonHelper.GetId(el) ?? string.Empty;
        return new FatianshiTemplateListItem(
            id,
            FatianshiJsonHelper.GetTitle(el) ?? id,
            FatianshiJsonHelper.GetString(el, "type", "templateType"),
            FatianshiJsonHelper.GetStringList(el, "categories", "categoryList"),
            FatianshiJsonHelper.GetStringList(el, "tags", "tagList"),
            FatianshiJsonHelper.GetUpdatedAt(el));
    }

    private static JsonElement UnwrapData(JsonElement root)
    {
        if (root.TryGetProperty("data", out var data)) return data;
        foreach (var prop in root.EnumerateObject())
        {
            if (string.Equals(prop.Name, "data", StringComparison.OrdinalIgnoreCase))
                return prop.Value;
        }
        return root;
    }

    private static string Truncate(string s) => s.Length <= 500 ? s : s[..500] + "...";
}
