using Cobryx.Domain.Identity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cobryx.Infrastructure.Persistence.Configurations;

public class BillingAlertConfiguration : IEntityTypeConfiguration<BillingAlert>
{
    public void Configure(EntityTypeBuilder<BillingAlert> builder)
    {
        builder.HasKey(a => a.Id);
        builder.HasIndex(a => a.TenantId);
        builder.Property(a => a.Message).IsRequired().HasMaxLength(1000);
    }
}
