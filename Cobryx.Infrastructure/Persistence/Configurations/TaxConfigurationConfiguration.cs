using Cobryx.Domain.Accounting;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cobryx.Infrastructure.Persistence.Configurations;

public class TaxConfigurationConfiguration : IEntityTypeConfiguration<TaxConfiguration>
{
    public void Configure(EntityTypeBuilder<TaxConfiguration> builder)
    {
        builder.HasIndex(t => t.TenantId);
        builder.HasIndex(t => new { t.TenantId, t.Name }).IsUnique();

        builder.Property(t => t.Name).IsRequired().HasMaxLength(100);
        builder.Property(t => t.Rate).HasPrecision(18, 4);
    }
}
