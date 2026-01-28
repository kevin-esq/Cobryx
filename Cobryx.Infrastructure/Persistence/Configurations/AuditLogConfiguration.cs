using Cobryx.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cobryx.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasIndex(a => a.TenantId);
        builder.HasIndex(a => a.EntityId);
        builder.HasIndex(a => a.CreatedAt);

        builder.Property(a => a.EntityName).IsRequired().HasMaxLength(100);
        builder.Property(a => a.Action).IsRequired().HasMaxLength(50);
        builder.Property(a => a.OldValues).HasColumnType("jsonb");
        builder.Property(a => a.NewValues).HasColumnType("jsonb");
    }
}
