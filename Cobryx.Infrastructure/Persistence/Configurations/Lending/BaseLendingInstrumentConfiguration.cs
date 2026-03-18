using Cobryx.Domain.Lending;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cobryx.Infrastructure.Persistence.Configurations.Lending;

public class BaseLendingInstrumentConfiguration : IEntityTypeConfiguration<BaseLendingInstrument>
{
    public void Configure(EntityTypeBuilder<BaseLendingInstrument> builder)
    {
        builder.ToTable("LendingInstruments");
        builder.HasKey(x => x.Id);


        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.TenantId, x.CreatedAt }).IsDescending(false, true);
        builder.HasIndex(x => new { x.TenantId, x.IsDeleted });

        builder.HasOne(x => x.Customer)
            .WithMany(x => x.LendingInstruments)
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);


        builder.OwnsOne(x => x.Principal, p =>
        {
            p.Property(m => m.Amount).HasPrecision(18, 2).HasColumnName("Principal_Amount");
            p.Property(m => m.Currency).HasMaxLength(3).HasColumnName("Principal_Currency");
        });

        builder.Property(x => x.InterestRate).HasPrecision(18, 4);
        builder.Property(x => x.InterestType).HasConversion<string>();
        builder.Property(x => x.Frequency).HasConversion<string>();

        builder.HasMany(x => x.Installments)
            .WithOne(x => x.Instrument)
            .HasForeignKey(x => x.InstrumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
