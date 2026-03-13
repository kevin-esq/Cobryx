using Cobryx.Domain.Lending;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cobryx.Infrastructure.Persistence.Configurations;

public class InstallmentConfiguration : IEntityTypeConfiguration<Installment>
{
    public void Configure(EntityTypeBuilder<Installment> builder)
    {
        builder.HasKey(i => i.Id);
        builder.HasIndex(i => i.InstrumentId);
        builder.HasIndex(i => i.DueDate);
        builder.HasIndex(i => i.Status);

        builder.HasOne(i => i.Instrument)
            .WithMany(l => l.Installments)
            .HasForeignKey(i => i.InstrumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(i => i.Number).IsRequired();

        builder.OwnsOne(i => i.TotalAmount, p =>
        {
            p.Property(m => m.Amount).HasPrecision(18, 2).HasColumnName("TotalAmount_Amount");
            p.Property(m => m.Currency).HasMaxLength(3).HasColumnName("TotalAmount_Currency");
        });

        builder.OwnsOne(i => i.PrincipalPart, p =>
        {
            p.Property(m => m.Amount).HasPrecision(18, 2).HasColumnName("PrincipalPart_Amount");
            p.Property(m => m.Currency).HasMaxLength(3).HasColumnName("PrincipalPart_Currency");
        });

        builder.OwnsOne(i => i.PrincipalPaid, p =>
        {
            p.Property(m => m.Amount).HasPrecision(18, 2).HasColumnName("PrincipalPaid_Amount");
            p.Property(m => m.Currency).HasMaxLength(3).HasColumnName("PrincipalPaid_Currency");
        });

        builder.OwnsOne(i => i.InterestPart, p =>
        {
            p.Property(m => m.Amount).HasPrecision(18, 2).HasColumnName("InterestPart_Amount");
            p.Property(m => m.Currency).HasMaxLength(3).HasColumnName("InterestPart_Currency");
        });

        builder.OwnsOne(i => i.InterestPaid, p =>
        {
            p.Property(m => m.Amount).HasPrecision(18, 2).HasColumnName("InterestPaid_Amount");
            p.Property(m => m.Currency).HasMaxLength(3).HasColumnName("InterestPaid_Currency");
        });

        builder.OwnsOne(i => i.LateInterestAmount, p =>
        {
            p.Property(m => m.Amount).HasPrecision(18, 2).HasColumnName("LateInterestAmount_Amount");
            p.Property(m => m.Currency).HasMaxLength(3).HasColumnName("LateInterestAmount_Currency");
        });

        builder.OwnsOne(i => i.LateInterestPaid, p =>
        {
            p.Property(m => m.Amount).HasPrecision(18, 2).HasColumnName("LateInterestPaid_Amount");
            p.Property(m => m.Currency).HasMaxLength(3).HasColumnName("LateInterestPaid_Currency");
        });

        builder.OwnsOne(i => i.RemainingBalance, p =>
        {
            p.Property(m => m.Amount).HasPrecision(18, 2).HasColumnName("RemainingBalance_Amount");
            p.Property(m => m.Currency).HasMaxLength(3).HasColumnName("RemainingBalance_Currency");
        });
    }
}
