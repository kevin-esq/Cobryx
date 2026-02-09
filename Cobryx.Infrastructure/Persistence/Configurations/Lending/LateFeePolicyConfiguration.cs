using Cobryx.Domain.Entities.Lending;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cobryx.Infrastructure.Persistence.Configurations.Lending;

/// <summary>
/// EF Core configuration for the <see cref="LateFeePolicy"/> entity.
/// </summary>
public class LateFeePolicyConfiguration : IEntityTypeConfiguration<LateFeePolicy>
{
    public void Configure(EntityTypeBuilder<LateFeePolicy> builder)
    {
        builder.ToTable("LateFeePolicies");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.TenantId, x.IsActive });

        builder.Property(x => x.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Type).IsRequired();

        builder.Property(x => x.Amount)
            .HasPrecision(18, 2);

        builder.Property(x => x.PercentageRate)
            .HasPrecision(18, 4);

        builder.Property(x => x.GracePeriodDays).IsRequired();

        builder.Property(x => x.MaxLateFeeAmount)
            .HasPrecision(18, 2);

        builder.Property(x => x.IsActive).IsRequired();
    }
}
