using ContractClause.Domain.Import;

namespace ContractClause.Application.Common.Interfaces;

public interface IImportTaskRepository
{
    Task<ImportTask?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(ImportTask task, CancellationToken ct = default);
    Task UpdateAsync(ImportTask task, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
