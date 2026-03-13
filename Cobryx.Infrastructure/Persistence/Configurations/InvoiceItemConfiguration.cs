using Cobryx.Domain.Accounting;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cobryx.Infrastructure.Persistence.Configurations;

public class InvoiceItemConfiguration : IEntityTypeConfiguration<InvoiceItem>
{
    public void Configure(EntityTypeBuilder<InvoiceItem> builder)
    {
        builder.Property(ii => ii.Description).IsRequired().HasMaxLength(500);
        builder.Property(ii => ii.Quantity).HasPrecision(18, 4);
        builder.Property(ii => ii.UnitPrice).HasPrecision(18, 4);
        builder.Property(ii => ii.TaxRate).HasPrecision(18, 4);

        builder.OwnsOne(ii => ii.Subtotal, sb =>
        {
            sb.Property(m => m.Amount).HasColumnName("SubtotalAmount").HasPrecision(18, 2);
            sb.Property(m => m.Currency).HasColumnName("SubtotalCurrency").HasMaxLength(10);
        });

        builder.OwnsOne(ii => ii.TaxAmount, tx =>
        {
            tx.Property(m => m.Amount).HasColumnName("TaxAmount").HasPrecision(18, 2);
            tx.Property(m => m.Currency).HasColumnName("TaxCurrency").HasMaxLength(10);
        });

        builder.OwnsOne(ii => ii.Total, tt =>
        {
            tt.Property(m => m.Amount).HasColumnName("TotalAmount").HasPrecision(18, 2);
            tt.Property(m => m.Currency).HasColumnName("TotalCurrency").HasMaxLength(10);
        });
    }
}
