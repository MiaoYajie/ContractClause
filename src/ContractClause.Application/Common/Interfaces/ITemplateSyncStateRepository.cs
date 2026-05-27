using ContractClause.Domain.Sync;

namespace ContractClause.Application.Common.Interfaces;

public interface ITemplateSyncStateRepository
{
    Task<TemplateSyncState> GetOrCreateAsync(CancellationToken ct = default);
    Task SaveAsync(TemplateSyncState state, CancellationToken ct = default);
}
