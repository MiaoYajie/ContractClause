using ContractClause.Domain.Clauses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContractClause.Infrastructure.Persistence.Configurations;

public class ClauseConfiguration : IEntityTypeConfiguration<Clause>
{
    public void Configure(EntityTypeBuilder<Clause> builder)
    {
        builder.ToTable("clauses");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Text).IsRequired();
        builder.Property(x => x.Variables).HasColumnType("text[]");
        builder.Property(x => x.Keywords).HasColumnType("text[]");
        builder.Property(x => x.ClauseType).HasMaxLength(100);
        builder.Property(x => x.OutlineItemId).HasMaxLength(50);
        builder.Property(x => x.VectorId).HasMaxLength(100);
    }
}
