using Cobryx.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cobryx.Infrastructure.Persistence.Configurations;

public class SystemErrorLogConfiguration : IEntityTypeConfiguration<SystemErrorLog>
{
    public void Configure(EntityTypeBuilder<SystemErrorLog> builder)
    {
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => x.IsResolved);

        builder.Property(x => x.Message).IsRequired();
        builder.Property(x => x.RequestPath).HasMaxLength(500);
        builder.Property(x => x.RequestMethod).HasMaxLength(10);
        builder.Property(x => x.IpAddress).HasMaxLength(50);
        builder.Property(x => x.ResolutionNotes).HasMaxLength(1000);
    }
}
