using ContractClause.Application.Common.Interfaces;
using ContractClause.Domain.Clauses;
using Microsoft.EntityFrameworkCore;

namespace ContractClause.Infrastructure.Persistence.Repositories;

public class ClauseRepository(AppDbContext db) : IClauseRepository
{
    public Task<Clause?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Clauses.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, ct);

    public async Task<IReadOnlyList<Clause>> GetByTemplateAndOutlineItemAsync(
        Guid templateId, string? outlineItemId, string? clauseType, int skip, int take, CancellationToken ct = default)
    {
        var q = db.Clauses.AsNoTracking().Where(c => !c.IsDeleted);
        if (templateId != Guid.Empty)
            q = q.Where(c => c.TemplateId == templateId);
        if (!string.IsNullOrWhiteSpace(outlineItemId))
            q = q.Where(c => c.OutlineItemId == outlineItemId);
        if (!string.IsNullOrWhiteSpace(clauseType))
            q = q.Where(c => c.ClauseType == clauseType);

        return await q.OrderBy(c => c.CreatedAt).Skip(skip).Take(take).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Clause>> SearchKeywordAsync(
        string query, Guid? templateId, string? clauseType, int skip, int take, CancellationToken ct = default)
    {
        var q = db.Clauses.AsNoTracking().Where(c => !c.IsDeleted);
        if (templateId.HasValue) q = q.Where(c => c.TemplateId == templateId);
        if (!string.IsNullOrWhiteSpace(clauseType)) q = q.Where(c => c.ClauseType == clauseType);
        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim().ToLowerInvariant();
            q = q.Where(c => EF.Functions.ILike(c.Text, $"%{term}%") ||
                             c.Keywords.Any(k => EF.Functions.ILike(k, $"%{term}%")));
        }

        return await q.OrderByDescending(c => c.UpdatedAt).Skip(skip).Take(take).ToListAsync(ct);
    }

    public async Task<int> CountKeywordAsync(
        string query, Guid? templateId, string? clauseType, CancellationToken ct = default)
    {
        var list = await SearchKeywordAsync(query, templateId, clauseType, 0, int.MaxValue, ct);
        return list.Count;
    }

    public async Task AddRangeAsync(IEnumerable<Clause> clauses, CancellationToken ct = default) =>
        await db.Clauses.AddRangeAsync(clauses, ct);

    public async Task SoftDeleteByTemplateAsync(Guid templateId, CancellationToken ct = default)
    {
        await db.Clauses.Where(c => c.TemplateId == templateId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.IsDeleted, true)
                .SetProperty(c => c.UpdatedAt, DateTime.UtcNow), ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
