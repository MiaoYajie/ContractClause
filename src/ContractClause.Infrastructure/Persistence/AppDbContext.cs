using ContractClause.Domain.ApiKeys;
using ContractClause.Domain.Clauses;
using ContractClause.Domain.Import;
using ContractClause.Domain.Sync;
using ContractClause.Domain.Templates;
using Microsoft.EntityFrameworkCore;

namespace ContractClause.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<Template> Templates => Set<Template>();
    public DbSet<Outline> Outlines => Set<Outline>();
    public DbSet<Clause> Clauses => Set<Clause>();
    public DbSet<ImportTask> ImportTasks => Set<ImportTask>();
    public DbSet<TemplateSyncState> TemplateSyncStates => Set<TemplateSyncState>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
