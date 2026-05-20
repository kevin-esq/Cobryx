using Cobryx.Domain.Idempotency;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cobryx.Infrastructure.Persistence.Configurations;

public class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable("idempotency_records");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.IdempotencyKey)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.RequestHash)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.ResponseBody)
            .HasMaxLength(65536);

        builder.Property(x => x.ContentType)
            .HasMaxLength(128);

        builder.Property(x => x.LocationHeader)
            .HasMaxLength(2048);

        builder.Property(x => x.ExternalGatewayId)
            .HasMaxLength(256);

        builder.Property(x => x.StatusCode)
            .IsRequired();

        builder.Property(x => x.ExpiresAt)
            .IsRequired();

        builder.Property(x => x.ProcessingStartedAt);

        builder.Property(x => x.ResourceType)
            .HasMaxLength(128);

        builder.Property(x => x.ResourceId);

        builder.Property(x => x.Environment)
            .HasMaxLength(32);

        builder.Property(x => x.CorrelationId);

        builder.Property(x => x.CausationId);

        // Unique constraint for idempotency enforcement
        builder.HasIndex(x => new { x.TenantId, x.IdempotencyKey })
            .IsUnique()
            .HasDatabaseName("ix_idempotency_records_tenant_key");

        // Index for gateway ID lookup (double-spend protection)
        builder.HasIndex(x => x.ExternalGatewayId)
            .HasDatabaseName("ix_idempotency_records_gateway_id")
            .HasFilter("\"ExternalGatewayId\" IS NOT NULL");

        // Index for cleanup of expired records
        builder.HasIndex(x => x.ExpiresAt)
            .HasDatabaseName("ix_idempotency_records_expires_at");
    }
}
