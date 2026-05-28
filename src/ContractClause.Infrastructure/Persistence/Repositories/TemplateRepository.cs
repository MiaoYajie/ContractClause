using ContractClause.Application.Common.Interfaces;
using ContractClause.Domain.Templates;
using Microsoft.EntityFrameworkCore;

namespace ContractClause.Infrastructure.Persistence.Repositories;

public class TemplateRepository(AppDbContext db) : ITemplateRepository
{
    private IQueryable<Template> Visible(Guid? ownerId) =>
        db.Templates.AsNoTracking()
            .Where(t => !t.IsDeleted && (t.OwnerId == null || t.IsOfficial || (ownerId != null && t.OwnerId == ownerId)));

    public Task<Template?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Templates.FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted, ct);

    public Task<Template?> GetByIdWithOutlineAsync(Guid id, CancellationToken ct = default) =>
        db.Templates.Include(t => t.Outline).FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted, ct);

    public async Task<DateTime?> GetMaxSourceUpdatedAtAsync(CancellationToken ct = default)
    {
        var max = await db.Templates
            .Where(t => !t.IsDeleted && t.SourceUpdatedAt != null)
            .MaxAsync(t => (DateTime?)t.SourceUpdatedAt, ct);
        return max;
    }

    public async Task<IReadOnlyList<Template>> SearchKeywordAsync(
        string query, string? type, IReadOnlyList<string>? categories, bool? isOfficial, Guid? ownerId,
        int skip, int take, CancellationToken ct = default)
    {
        var q = Visible(ownerId);
        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim().ToLowerInvariant();
            q = q.Where(t =>
                EF.Functions.ILike(t.Title, $"%{term}%") ||
                EF.Functions.ILike(t.Summary, $"%{term}%") ||
                EF.Functions.ILike(t.Scenarios, $"%{term}%"));
        }
        if (!string.IsNullOrWhiteSpace(type)) q = q.Where(t => t.Type == type);
        if (isOfficial.HasValue) q = q.Where(t => t.IsOfficial == isOfficial.Value);
        if (categories is { Count: > 0 })
            q = q.Where(t => t.Categories.Any(c => categories.Contains(c)));

        return await q.OrderByDescending(t => t.UpdatedAt).Skip(skip).Take(take).ToListAsync(ct);
    }

    public async Task<int> CountKeywordAsync(
        string query, string? type, IReadOnlyList<string>? categories, bool? isOfficial, Guid? ownerId,
        CancellationToken ct = default)
    {
        var list = await SearchKeywordAsync(query, type, categories, isOfficial, ownerId, 0, int.MaxValue, ct);
        return list.Count;
    }

    public async Task<int> GetNextNumberAsync(CancellationToken ct = default)
    {
        var max = await db.Templates.MaxAsync(t => (int?)t.Number, ct) ?? 100;
        return max + 1;
    }

    public async Task AddAsync(Template template, CancellationToken ct = default) =>
        await db.Templates.AddAsync(template, ct);

    public Task UpdateAsync(Template template, CancellationToken ct = default)
    {
        db.Templates.Update(template);
        return Task.CompletedTask;
    }

    public async Task SoftDeleteAsync(Guid id, CancellationToken ct = default)
    {
        var template = await db.Templates.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (template is null) return;
        template.IsDeleted = true;
        template.UpdatedAt = DateTime.UtcNow;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
