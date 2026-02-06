using Cobryx.Domain.Entities.Invoicing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cobryx.Infrastructure.Persistence.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.HasIndex(i => i.TenantId);
        builder.HasIndex(i => i.CustomerId);
        builder.HasIndex(i => new { i.TenantId, i.InvoiceNumber }).IsUnique();

        builder.Property(i => i.InvoiceNumber).IsRequired().HasMaxLength(50);
        builder.Property(i => i.Notes).HasMaxLength(1000);

        builder.OwnsOne(i => i.Subtotal, sb =>
        {
            sb.Property(m => m.Amount).HasColumnName("SubtotalAmount").HasPrecision(18, 2);
            sb.Property(m => m.Currency).HasColumnName("SubtotalCurrency").HasMaxLength(10);
        });

        builder.OwnsOne(i => i.TaxAmount, tx =>
        {
            tx.Property(m => m.Amount).HasColumnName("TaxAmount").HasPrecision(18, 2);
            tx.Property(m => m.Currency).HasColumnName("TaxCurrency").HasMaxLength(10);
        });

        builder.OwnsOne(i => i.Total, tt =>
        {
            tt.Property(m => m.Amount).HasColumnName("TotalAmount").HasPrecision(18, 2);
            tt.Property(m => m.Currency).HasColumnName("TotalCurrency").HasMaxLength(10);
        });

        builder.OwnsOne(i => i.TotalPaid, tp =>
        {
            tp.Property(m => m.Amount).HasColumnName("TotalPaidAmount").HasPrecision(18, 2);
            tp.Property(m => m.Currency).HasColumnName("TotalPaidCurrency").HasMaxLength(10);
        });

        builder.HasMany(i => i.Items)
            .WithOne()
            .HasForeignKey(ii => ii.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
