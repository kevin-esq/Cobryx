using Cobryx.Domain.Entities.Accounting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cobryx.Infrastructure.Persistence.Configurations.Accounting;

public class LedgerEntryConfiguration : IEntityTypeConfiguration<LedgerEntry>
{
    public void Configure(EntityTypeBuilder<LedgerEntry> builder)
    {
        builder.ToTable("LedgerEntries");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Debit)
            .HasPrecision(18, 4);

        builder.Property(x => x.Credit)
            .HasPrecision(18, 4);

        builder.HasIndex(x => new { x.TenantId, x.Id });
        builder.HasIndex(x => new { x.TenantId, x.AccountId, x.CreatedAt });
        builder.HasIndex(x => new { x.TenantId, x.CreatedAt });

        builder.HasIndex(x => x.TransactionId);
    }
}
