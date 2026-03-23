using Cobryx.Domain.Messaging;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cobryx.Infrastructure.Persistence.Configurations;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Payload).IsRequired();
        builder.Property(x => x.CorrelationId).HasMaxLength(100);
        builder.Property(x => x.PartitionKey).HasMaxLength(100);

        builder.HasIndex(x => new { x.IsProcessed, x.OccurredOnUtc })
               .HasDatabaseName("IX_OutboxMessages_GenericQueue");

        builder.HasIndex(x => new { x.IsProcessed, x.LedgerSequenceId })
               .HasDatabaseName("IX_OutboxMessages_LedgerQueue");

        // Indexes for performance/auditing
        builder.HasIndex(x => x.CorrelationId);
        builder.HasIndex(x => x.EntityId);
    }
}
