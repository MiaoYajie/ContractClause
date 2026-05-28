using ContractClause.Application.Common.Interfaces;
using ContractClause.Domain.ApiKeys;
using Microsoft.EntityFrameworkCore;

namespace ContractClause.Infrastructure.Persistence.Repositories;

public class ApiKeyRepository(AppDbContext db) : IApiKeyRepository
{
    public Task<ApiKey?> GetByKeyAsync(string key, CancellationToken ct = default) =>
        db.ApiKeys.AsNoTracking().FirstOrDefaultAsync(k => k.Key == key && !k.IsDeleted, ct);

    public async Task<IReadOnlyList<ApiKey>> GetByOwnerIdAsync(Guid ownerId, CancellationToken ct = default) =>
        await db.ApiKeys.AsNoTracking()
            .Where(k => k.OwnerId == ownerId && !k.IsDeleted)
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync(ct);

    public async Task<ApiKey> CreateAsync(Guid ownerId, string ownerType, CancellationToken ct = default)
    {
        var entity = new ApiKey
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            OwnerType = ownerType,
            Key = $"sk-{Guid.NewGuid():N}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await db.ApiKeys.AddAsync(entity, ct);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task<bool> SoftDeleteAsync(Guid id, Guid ownerId, CancellationToken ct = default)
    {
        var entity = await db.ApiKeys.FirstOrDefaultAsync(k => k.Id == id && k.OwnerId == ownerId && !k.IsDeleted, ct);
        if (entity is null)
            return false;

        entity.IsDeleted = true;
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return true;
    }
}
