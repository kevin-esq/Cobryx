using Cobryx.Domain.Lending;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cobryx.Infrastructure.Persistence.Configurations.Lending;

/// <summary>
/// EF Core configuration for the <see cref="PaymentApplicationPolicy"/> entity.
/// </summary>
public class PaymentApplicationPolicyConfiguration : IEntityTypeConfiguration<PaymentApplicationPolicy>
{
    public void Configure(EntityTypeBuilder<PaymentApplicationPolicy> builder)
    {
        builder.ToTable("PaymentApplicationPolicies");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.TenantId, x.IsActive });
        builder.HasIndex(x => new { x.TenantId, x.IsDefault });

        builder.Property(x => x.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Mode).IsRequired();

        builder.Property(x => x.ApplicationOrderJson)
            .HasMaxLength(500);

        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.IsDefault).IsRequired();
    }
}
