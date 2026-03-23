using Cobryx.Domain.Payments;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cobryx.Infrastructure.Persistence.Configurations;

public class PaymentLinkConfiguration : IEntityTypeConfiguration<PaymentLink>
{
    public void Configure(EntityTypeBuilder<PaymentLink> builder)
    {
        builder.HasIndex(p => p.TenantId);
        builder.HasIndex(p => p.CustomerId);

        // Fintech hardening: Unique hash index to prevent token collisions
        builder.HasIndex(p => p.TokenHash).IsUnique();

        builder.HasIndex(p => p.Status);
        builder.HasIndex(p => new { p.Status, p.UpdatedAt });

        // Idempotency: Unified unique index for external references per tenant
        builder.HasIndex(p => new { p.TenantId, p.ExternalReference })
            .IsUnique()
            .HasFilter("\"ExternalReference\" IS NOT NULL");

        // Stripe Hardening: Prevent multiple links from sharing the same intent
        builder.HasIndex(p => p.StripePaymentIntentId)
            .IsUnique()
            .HasFilter("\"StripePaymentIntentId\" IS NOT NULL");

        builder.ComplexProperty(p => p.AmountSnapshot, m =>
        {
            m.Property(x => x.Amount).HasColumnName("AmountSnapshot_Value").HasPrecision(18, 2);
            m.Property(x => x.Currency).HasColumnName("AmountSnapshot_Currency").HasMaxLength(3);
        });

        builder.ComplexProperty(p => p.PaidAmount, m =>
        {
            m.Property(x => x.Amount).HasColumnName("PaidAmount_Value").HasPrecision(18, 2);
            m.Property(x => x.Currency).HasColumnName("PaidAmount_Currency").HasMaxLength(3);
        });

        builder.Property(p => p.TokenHash).IsRequired().HasMaxLength(256);
        builder.Property(p => p.ExternalReference).HasMaxLength(100);
        builder.Property(p => p.StripePaymentIntentId).HasMaxLength(100);
    }
}
