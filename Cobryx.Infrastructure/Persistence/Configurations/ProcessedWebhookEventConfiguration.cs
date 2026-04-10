using Cobryx.Domain.Webhooks;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cobryx.Infrastructure.Persistence.Configurations;

public class ProcessedWebhookEventConfiguration : IEntityTypeConfiguration<ProcessedWebhookEvent>
{
    public void Configure(EntityTypeBuilder<ProcessedWebhookEvent> builder)
    {
        builder.ToTable("processed_webhook_events");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Provider)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.EventId)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.EventType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.PaymentIntentId)
            .HasMaxLength(256);

        builder.Property(e => e.RelatedEntityType)
            .HasMaxLength(100);

        builder.Property(e => e.ProcessedAt)
            .IsRequired();

        // CRITICAL: Unique constraint for exactly-once semantics
        builder.HasIndex(e => new { e.Provider, e.EventId })
            .IsUnique()
            .HasDatabaseName("ix_processed_webhook_events_provider_event");

        // Index for payment intent lookup (API vs webhook race protection)
        builder.HasIndex(e => e.PaymentIntentId)
            .HasDatabaseName("ix_processed_webhook_events_payment_intent")
            .HasFilter("\"PaymentIntentId\" IS NOT NULL");

        // Index for related entity lookup
        builder.HasIndex(e => new { e.RelatedEntityType, e.RelatedEntityId })
            .HasDatabaseName("ix_processed_webhook_events_related_entity")
            .HasFilter("\"RelatedEntityId\" IS NOT NULL");
    }
}
