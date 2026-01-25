using Cobryx.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cobryx.Infrastructure.Persistence.Configurations;

public class InstallmentConfiguration : IEntityTypeConfiguration<Installment>
{
    public void Configure(EntityTypeBuilder<Installment> builder)
    {
        builder.OwnsOne(i => i.TotalAmount, p =>
        {
            p.Property(m => m.Amount).HasPrecision(18, 2).HasColumnName("TotalAmount");
            p.Property(m => m.Currency).HasMaxLength(3);
        });

        builder.OwnsOne(i => i.PrincipalPart, p =>
        {
            p.Property(m => m.Amount).HasPrecision(18, 2).HasColumnName("PrincipalPart");
            p.Property(m => m.Currency).HasMaxLength(3);
        });

        builder.OwnsOne(i => i.PrincipalPaid, p =>
        {
            p.Property(m => m.Amount).HasPrecision(18, 2).HasColumnName("PrincipalPaid");
            p.Property(m => m.Currency).HasMaxLength(3);
        });

        builder.OwnsOne(i => i.InterestPart, p =>
        {
            p.Property(m => m.Amount).HasPrecision(18, 2).HasColumnName("InterestPart");
            p.Property(m => m.Currency).HasMaxLength(3);
        });

        builder.OwnsOne(i => i.InterestPaid, p =>
        {
            p.Property(m => m.Amount).HasPrecision(18, 2).HasColumnName("InterestPaid");
            p.Property(m => m.Currency).HasMaxLength(3);
        });

        builder.OwnsOne(i => i.LateInterestAmount, p =>
        {
            p.Property(m => m.Amount).HasPrecision(18, 2).HasColumnName("LateInterestAmount");
            p.Property(m => m.Currency).HasMaxLength(3);
        });

        builder.OwnsOne(i => i.LateInterestPaid, p =>
        {
            p.Property(m => m.Amount).HasPrecision(18, 2).HasColumnName("LateInterestPaid");
            p.Property(m => m.Currency).HasMaxLength(3);
        });

        builder.OwnsOne(i => i.RemainingBalance, p =>
        {
            p.Property(m => m.Amount).HasPrecision(18, 2).HasColumnName("RemainingBalance");
            p.Property(m => m.Currency).HasMaxLength(3);
        });
    }
}
