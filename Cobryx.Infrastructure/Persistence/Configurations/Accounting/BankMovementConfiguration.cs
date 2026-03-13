using Cobryx.Domain.Accounting;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cobryx.Infrastructure.Persistence.Configurations.Accounting;

public class BankMovementConfiguration : IEntityTypeConfiguration<BankMovement>
{
    public void Configure(EntityTypeBuilder<BankMovement> builder)
    {
        builder.ToTable("BankMovements");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Amount)
            .HasPrecision(18, 2);

        builder.Property(x => x.MatchingConfidence)
            .HasPrecision(5, 4);

        builder.Property(x => x.Direction)
            .HasConversion<string>();

        builder.Property(x => x.Status)
            .HasConversion<string>();

        builder.HasIndex(x => new { x.TenantId, x.BookingDate });
        builder.HasIndex(x => new { x.TenantId, x.ProviderTransactionId }).IsUnique();
        builder.HasIndex(x => x.Status);
    }
}
