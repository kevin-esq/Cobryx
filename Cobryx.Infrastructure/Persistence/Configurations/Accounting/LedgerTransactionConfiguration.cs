using Cobryx.Domain.Entities.Accounting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cobryx.Infrastructure.Persistence.Configurations.Accounting;

public class LedgerTransactionConfiguration : IEntityTypeConfiguration<LedgerTransaction>
{
    public void Configure(EntityTypeBuilder<LedgerTransaction> builder)
    {
        builder.ToTable("LedgerTransactions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Description)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.ReferenceId)
            .HasMaxLength(100);


        builder.Property(x => x.IsPosted)
            .IsRequired();

        builder.Property(x => x.IsReversal)
            .IsRequired();

        builder.HasMany(x => x.Entries)
            .WithOne()
            .HasForeignKey(x => x.TransactionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.TenantId, x.ReferenceId })
            .IsUnique()
            .HasFilter("\"ReferenceId\" IS NOT NULL");
        builder.HasIndex(x => x.CreatedAt);
    }
}
