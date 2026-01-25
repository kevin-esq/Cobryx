using Cobryx.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cobryx.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.OwnsOne(p => p.BasePrice, m =>
        {
            m.Property(x => x.Amount).HasPrecision(18, 2).HasColumnName("BasePrice");
            m.Property(x => x.Currency).HasMaxLength(3);
        });

        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Sku).HasMaxLength(50);
    }
}
