using ContractClause.Domain.Templates;

namespace ContractClause.Application.Common.Interfaces;

public interface ITemplateRepository
{
    Task<Template?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Template?> GetByIdWithOutlineAsync(Guid id, CancellationToken ct = default);
    Task<DateTime?> GetMaxSourceUpdatedAtAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Template>> SearchKeywordAsync(string query, string? type, IReadOnlyList<string>? categories, bool? isOfficial, Guid? ownerId, int skip, int take, CancellationToken ct = default);
    Task<int> CountKeywordAsync(string query, string? type, IReadOnlyList<string>? categories, bool? isOfficial, Guid? ownerId, CancellationToken ct = default);
    Task<int> GetNextNumberAsync(CancellationToken ct = default);
    Task AddAsync(Template template, CancellationToken ct = default);
    Task UpdateAsync(Template template, CancellationToken ct = default);
    Task SoftDeleteAsync(Guid id, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
