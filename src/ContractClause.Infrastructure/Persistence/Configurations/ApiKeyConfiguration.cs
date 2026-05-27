using ContractClause.Domain.ApiKeys;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContractClause.Infrastructure.Persistence.Configurations;

public class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
{
    public void Configure(EntityTypeBuilder<ApiKey> builder)
    {
        builder.ToTable("api_keys");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.Key).IsUnique();
        builder.Property(x => x.Key).HasColumnName("api_key").HasMaxLength(200).IsRequired();
        builder.Property(x => x.OwnerType).HasMaxLength(50);
    }
}
