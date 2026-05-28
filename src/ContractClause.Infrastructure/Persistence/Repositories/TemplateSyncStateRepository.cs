using ContractClause.Application.Common.Interfaces;
using ContractClause.Domain.Sync;
using Microsoft.EntityFrameworkCore;

namespace ContractClause.Infrastructure.Persistence.Repositories;

public class TemplateSyncStateRepository(AppDbContext db) : ITemplateSyncStateRepository
{
    public async Task<TemplateSyncState> GetOrCreateAsync(CancellationToken ct = default)
    {
        var state = await db.TemplateSyncStates.FirstOrDefaultAsync(s => s.Id == TemplateSyncState.SingletonId, ct);
        if (state is not null) return state;

        state = new TemplateSyncState
        {
            Id = TemplateSyncState.SingletonId,
            LastRunErrors = [],
            UpdatedAt = DateTime.UtcNow
        };
        await db.TemplateSyncStates.AddAsync(state, ct);
        return state;
    }

    public async Task SaveAsync(TemplateSyncState state, CancellationToken ct = default)
    {
        state.UpdatedAt = DateTime.UtcNow;
        var entry = db.Entry(state);
        if (entry.State == EntityState.Detached)
            db.TemplateSyncStates.Update(state);
        await db.SaveChangesAsync(ct);
    }

    public async Task<DateTime?> GetLastRunAtAsync(CancellationToken ct = default)
    {
        var max = await db.TemplateSyncStates
            .Where(t=>t.LastRunAt != null)
            .MaxAsync(t => (DateTime?)t.LastRunAt, ct);
        return max;
    }
}
