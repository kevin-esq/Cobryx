using Cobryx.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cobryx.Infrastructure.Persistence.Configurations;

public class DocumentMetadataConfiguration : IEntityTypeConfiguration<DocumentMetadata>
{
    public void Configure(EntityTypeBuilder<DocumentMetadata> builder)
    {
        builder.HasKey(d => d.Id);
        builder.HasIndex(d => d.TenantId);
        builder.HasIndex(d => new { d.EntityId, d.EntityType });

        builder.Property(d => d.EntityType).IsRequired().HasMaxLength(50);
        builder.Property(d => d.FileName).IsRequired().HasMaxLength(255);
        builder.Property(d => d.BlobPath).IsRequired().HasMaxLength(1000);
        builder.Property(d => d.MimeType).IsRequired().HasMaxLength(100);
    }
}
