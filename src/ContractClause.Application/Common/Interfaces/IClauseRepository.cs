using ContractClause.Domain.Clauses;

namespace ContractClause.Application.Common.Interfaces;

public interface IClauseRepository
{
    Task<Clause?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Clause>> GetByTemplateAndOutlineItemAsync(Guid templateId, string? outlineItemId, string? clauseType, int skip, int take, CancellationToken ct = default);
    Task<IReadOnlyList<Clause>> ListByTemplateIdAsync(Guid templateId, CancellationToken ct = default);
    Task<IReadOnlyList<Clause>> SearchKeywordAsync(string query, Guid? templateId, string? clauseType, int skip, int take, CancellationToken ct = default);
    Task<int> CountKeywordAsync(string query, Guid? templateId, string? clauseType, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<Clause> clauses, CancellationToken ct = default);
    Task SoftDeleteByTemplateAsync(Guid templateId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
