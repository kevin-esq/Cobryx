using Cobryx.Domain.Entities.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cobryx.Infrastructure.Persistence.Configurations;

public class PaymentAllocationConfiguration : IEntityTypeConfiguration<PaymentAllocation>
{
    public void Configure(EntityTypeBuilder<PaymentAllocation> builder)
    {
        builder.HasIndex(pa => pa.PaymentId);
        builder.HasIndex(pa => pa.InvoiceId);

        builder.OwnsOne(pa => pa.Amount, a =>
        {
            a.Property(m => m.Amount).HasColumnName("Amount").HasPrecision(18, 2);
            a.Property(m => m.Currency).HasColumnName("Currency").HasMaxLength(10);
        });
    }
}
