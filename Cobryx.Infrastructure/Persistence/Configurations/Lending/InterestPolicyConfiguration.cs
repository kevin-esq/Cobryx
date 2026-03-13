using Cobryx.Domain.Lending;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cobryx.Infrastructure.Persistence.Configurations.Lending;

/// <summary>
/// EF Core configuration for the <see cref="InterestPolicy"/> entity.
/// </summary>
public class InterestPolicyConfiguration : IEntityTypeConfiguration<InterestPolicy>
{
    public void Configure(EntityTypeBuilder<InterestPolicy> builder)
    {
        builder.ToTable("InterestPolicies");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.TenantId, x.IsActive });

        builder.Property(x => x.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Type).IsRequired();
        builder.Property(x => x.Method).IsRequired();

        builder.Property(x => x.Rate)
            .HasPrecision(18, 4);

        builder.Property(x => x.CompoundingFrequency);

        builder.Property(x => x.CashPrice)
            .HasPrecision(18, 2);

        builder.Property(x => x.CreditPrice)
            .HasPrecision(18, 2);

        builder.Property(x => x.IsActive).IsRequired();
    }
}
