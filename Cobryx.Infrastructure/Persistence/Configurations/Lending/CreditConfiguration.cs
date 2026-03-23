using Cobryx.Domain.Lending;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cobryx.Infrastructure.Persistence.Configurations.Lending;

public class CreditConfiguration : IEntityTypeConfiguration<Credit>
{
    public void Configure(EntityTypeBuilder<Credit> builder)
    {
        builder.ToTable("Credits");
        builder.HasBaseType<BaseLendingInstrument>();



        builder.Property(x => x.StartDate).IsRequired();
        builder.Property(x => x.Status).IsRequired();

        builder.HasMany(x => x.Payments)
            .WithOne()
            .HasForeignKey("CreditId")
            .OnDelete(DeleteBehavior.Restrict);

        // via builder.HasMany(x => x.Installments).WithOne(x => x.Instrument).HasForeignKey(x => x.CreditId)
    }
}
