using ContractClause.Domain.ApiKeys;

namespace ContractClause.Application.Common.Interfaces;

public interface IApiKeyRepository
{
    Task<ApiKey?> GetByKeyAsync(string key, CancellationToken ct = default);
    Task<IReadOnlyList<ApiKey>> GetByOwnerIdAsync(Guid ownerId, CancellationToken ct = default);
    Task<ApiKey> CreateAsync(Guid ownerId, string ownerType, CancellationToken ct = default);
    Task<bool> SoftDeleteAsync(Guid id, Guid ownerId, CancellationToken ct = default);
}
