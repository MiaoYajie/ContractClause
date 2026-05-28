using ContractClause.Domain.Import;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContractClause.Infrastructure.Persistence.Configurations;

public class ImportTaskConfiguration : IEntityTypeConfiguration<ImportTask>
{
    public void Configure(EntityTypeBuilder<ImportTask> builder)
    {
        builder.ToTable("import_tasks");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Status).HasMaxLength(50);
        builder.Property(x => x.Errors)
            .HasColumnType("text[]")
            .HasConversion(
                v => v,
                v => v ?? new List<string>())
            .HasDefaultValueSql("'{}'");
    }
}
