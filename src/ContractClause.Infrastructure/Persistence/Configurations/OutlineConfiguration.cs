using ContractClause.Domain.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContractClause.Infrastructure.Persistence.Configurations;

public class OutlineConfiguration : IEntityTypeConfiguration<Outline>
{
    public void Configure(EntityTypeBuilder<Outline> builder)
    {
        builder.ToTable("outlines");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.TemplateId).IsUnique();
        builder.Property(x => x.OutlineJson).IsRequired();
    }
}
