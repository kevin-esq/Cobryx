using Cobryx.Domain.Collections;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cobryx.Infrastructure.Persistence.Configurations;

public class CollectionAgentConfiguration : IEntityTypeConfiguration<CollectionAgent>
{
    public void Configure(EntityTypeBuilder<CollectionAgent> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.MaxCapacity).IsRequired();
        builder.Property(x => x.CurrentLoad).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.IsActive });
    }
}
