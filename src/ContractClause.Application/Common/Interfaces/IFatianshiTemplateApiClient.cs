namespace ContractClause.Application.Common.Interfaces;

public interface IFatianshiTemplateApiClient
{
    /// <summary>
    /// 拉取推荐模板列表（按更新时间倒序）。page 从 0 开始。
    /// </summary>
    Task<FatianshiTemplatePage> FetchUpdatedTemplatesPageAsync(int page, CancellationToken ct = default);
}

public record FatianshiTemplatePage(IReadOnlyList<FatianshiTemplateMetadata> Items, int Total);

public record FatianshiTemplateMetadata(
    Guid Id,
    int? Number,
    string Title,
    string Alias,
    string Type,
    IReadOnlyList<string> Categories,
    IReadOnlyList<string> Tags,
    string Summary,
    string Scenarios,
    DateTime PublishedOn,
    int Version);
