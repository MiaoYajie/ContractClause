namespace ContractClause.Application.Common.Interfaces;

public interface IFatianshiTemplateApiClient
{
    Task<IReadOnlyList<FatianshiTemplateListItem>> SearchUpdatedAsync(
        DateTime? updatedAfter,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<FatianshiTemplateDetail> GetTemplateAsync(string externalId, CancellationToken ct = default);
}

public record FatianshiTemplateListItem(
    string Id,
    string Title,
    string? Type,
    IReadOnlyList<string> Categories,
    IReadOnlyList<string> Tags,
    DateTime? UpdatedAt);

public record FatianshiTemplateDetail(
    string Id,
    string Title,
    string Html,
    string? Type,
    IReadOnlyList<string> Categories,
    IReadOnlyList<string> Tags,
    DateTime? UpdatedAt);
