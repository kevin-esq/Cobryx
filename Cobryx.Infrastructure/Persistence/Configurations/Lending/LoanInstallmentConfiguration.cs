using Cobryx.Domain.Entities.Lending;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cobryx.Infrastructure.Persistence.Configurations.Lending;

/// <summary>
/// EF Core configuration for the <see cref="Installment"/> entity in the Lending domain.
/// </summary>
public class LoanInstallmentConfiguration : IEntityTypeConfiguration<Installment>
{
    public void Configure(EntityTypeBuilder<Installment> builder)
    {
        builder.ToTable("LoanInstallments");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.LoanId);
        builder.HasIndex(x => x.DueDate);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => new { x.LoanId, x.InstallmentNumber });

        builder.Property(x => x.InstallmentNumber).IsRequired();
        builder.Property(x => x.DueDate).IsRequired();

        builder.Property(x => x.PrincipalAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.InterestAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.PrincipalPaid)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.InterestPaid)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.LateFeePaid)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.PaidAt);
    }
}
