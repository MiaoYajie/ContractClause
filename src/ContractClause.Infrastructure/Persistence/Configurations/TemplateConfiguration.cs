using ContractClause.Domain.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContractClause.Infrastructure.Persistence.Configurations;

public class TemplateConfiguration : IEntityTypeConfiguration<Template>
{
    public void Configure(EntityTypeBuilder<Template> builder)
    {
        builder.ToTable("templates");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.Number).IsUnique();
        builder.Property(x => x.Title).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Alias).HasMaxLength(500);
        builder.Property(x => x.Type).HasMaxLength(100);
        builder.Property(x => x.Categories).HasColumnType("text[]");
        builder.Property(x => x.Tags).HasColumnType("text[]");
        builder.Property(x => x.Summary);
        builder.Property(x => x.Scenarios);
        builder.HasOne(x => x.Outline).WithOne(x => x.Template).HasForeignKey<Outline>(x => x.TemplateId);
        builder.HasMany(x => x.Clauses).WithOne(x => x.Template).HasForeignKey(x => x.TemplateId);
    }
}
