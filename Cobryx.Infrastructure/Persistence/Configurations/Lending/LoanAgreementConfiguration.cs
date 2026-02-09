using Cobryx.Domain.Entities.Lending;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cobryx.Infrastructure.Persistence.Configurations.Lending;

/// <summary>
/// EF Core configuration for the <see cref="LoanAgreement"/> entity.
/// </summary>
public class LoanAgreementConfiguration : IEntityTypeConfiguration<LoanAgreement>
{
    public void Configure(EntityTypeBuilder<LoanAgreement> builder)
    {
        builder.ToTable("LoanAgreements");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.CustomerId);
        builder.HasIndex(x => new { x.TenantId, x.StartDate });

        builder.Property(x => x.PrincipalAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.Currency)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(x => x.NumberOfInstallments).IsRequired();
        builder.Property(x => x.StartDate).IsRequired();
        builder.Property(x => x.FirstPaymentDate).IsRequired();
        builder.Property(x => x.DaysBetweenPayments);
        builder.Property(x => x.Origin).IsRequired();
        builder.Property(x => x.PaymentFrequency).IsRequired();
        builder.Property(x => x.RoundingMode).IsRequired();
        builder.Property(x => x.IsSigned).IsRequired();
        builder.Property(x => x.SignedAt);
    }
}
