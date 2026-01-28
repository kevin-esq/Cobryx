using Cobryx.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cobryx.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasIndex(p => p.TenantId);
        builder.HasIndex(p => p.Sku);
        builder.HasIndex(p => new { p.TenantId, p.Name });

        builder.Property(p => p.Name).IsRequired().HasMaxLength(100);
        builder.Property(p => p.Sku).HasMaxLength(50);

        builder.OwnsOne(p => p.BasePrice, m =>
        {
            m.Property(m => m.Amount).HasPrecision(18, 2);
            m.Property(m => m.Currency).HasMaxLength(3);
        });
    }
}
