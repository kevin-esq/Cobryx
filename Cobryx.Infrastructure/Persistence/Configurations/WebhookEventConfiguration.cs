using Cobryx.Application.Webhooks.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cobryx.Infrastructure.Persistence.Configurations;

public class WebhookEventConfiguration : IEntityTypeConfiguration<WebhookEvent>
{
    public void Configure(EntityTypeBuilder<WebhookEvent> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Provider)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.ExternalEventId)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(e => e.RawPayload)
            .IsRequired()
            .HasColumnType("jsonb");

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasIndex(e => new { e.Provider, e.ExternalEventId })
            .IsUnique();

        builder.Property(e => e.Error)
            .HasMaxLength(2000);
    }
}
