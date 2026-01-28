using Cobryx.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cobryx.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.HasIndex(p => p.TenantId);
        builder.HasIndex(p => p.CustomerId);
        builder.HasIndex(p => p.PaymentMethodId);

        builder.OwnsOne(p => p.Amount, m =>
        {
            m.Property(x => x.Amount).HasPrecision(18, 2);
            m.Property(x => x.Currency).HasMaxLength(3);
        });

        builder.HasMany(p => p.Allocations)
            .WithOne()
            .HasForeignKey(pa => pa.PaymentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
