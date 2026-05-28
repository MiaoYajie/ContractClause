using ContractClause.Domain.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContractClause.Infrastructure.Persistence.Configurations;

public class TemplateSyncStateConfiguration : IEntityTypeConfiguration<TemplateSyncState>
{
    public void Configure(EntityTypeBuilder<TemplateSyncState> builder)
    {
        builder.ToTable("template_sync_state");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.LastRunStatus).HasMaxLength(50);
        builder.Property(x => x.LastRunErrors)
            .HasColumnType("text[]")
            .HasConversion(
                v => v,
                v => v ?? new List<string>())
            .HasDefaultValueSql("'{}'");
    }
}
