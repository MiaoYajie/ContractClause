using ContractClause.Application.Common.Interfaces;
using ContractClause.Domain.Import;
using Microsoft.EntityFrameworkCore;

namespace ContractClause.Infrastructure.Persistence.Repositories;

public class ImportTaskRepository(AppDbContext db) : IImportTaskRepository
{
    public Task<ImportTask?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.ImportTasks.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task AddAsync(ImportTask task, CancellationToken ct = default) =>
        await db.ImportTasks.AddAsync(task, ct);

    public Task UpdateAsync(ImportTask task, CancellationToken ct = default)
    {
        db.ImportTasks.Update(task);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
