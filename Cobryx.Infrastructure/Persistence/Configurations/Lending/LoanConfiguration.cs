using Cobryx.Domain.Lending;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cobryx.Infrastructure.Persistence.Configurations.Lending;

/// <summary>
/// EF Core configuration for the <see cref="Loan"/> entity.
/// </summary>
public class LoanConfiguration : IEntityTypeConfiguration<Loan>
{
    public void Configure(EntityTypeBuilder<Loan> builder)
    {
        builder.ToTable("Loans");
        builder.HasBaseType<BaseLendingInstrument>();

        builder.HasIndex(x => x.LoanAgreementId);

        builder.Property(x => x.LoanNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.CurrentPrincipalBalance)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.CurrentInterestBalance)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.CurrentLateFeeBalance)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.TotalPaid)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.RiskStatus).IsRequired();
        builder.Property(x => x.CollectionStage).IsRequired();
        builder.Property(x => x.DaysInArrears).IsRequired();

        builder.HasOne(x => x.Agreement)
            .WithOne(x => x.Loan)
            .HasForeignKey<Loan>(x => x.LoanAgreementId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
