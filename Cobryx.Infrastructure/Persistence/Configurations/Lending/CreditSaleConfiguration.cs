using Cobryx.Domain.Lending;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cobryx.Infrastructure.Persistence.Configurations.Lending;

/// <summary>
/// EF Core configuration for the <see cref="CreditSale"/> entity.
/// </summary>
public class CreditSaleConfiguration : IEntityTypeConfiguration<CreditSale>
{
    public void Configure(EntityTypeBuilder<CreditSale> builder)
    {
        builder.ToTable("CreditSales");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.CustomerId);
        builder.HasIndex(x => x.LoanId);
        builder.HasIndex(x => new { x.TenantId, x.SaleDate });

        builder.Property(x => x.ProductDescription)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.ProductCategory)
            .HasMaxLength(100);

        builder.Property(x => x.CashPrice)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.CreditPrice)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.DownPayment)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.SaleDate).IsRequired();

        builder.HasOne(x => x.Agreement)
            .WithMany()
            .HasForeignKey(x => x.LoanAgreementId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Loan)
            .WithMany()
            .HasForeignKey(x => x.LoanId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
