using Cobryx.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cobryx.Infrastructure.Persistence.Configurations;

public class TenantGrowthMetricsConfiguration : IEntityTypeConfiguration<TenantGrowthMetrics>
{
    public void Configure(EntityTypeBuilder<TenantGrowthMetrics> builder)
    {
        builder.ToTable("TenantGrowthMetrics");

        builder.HasKey(x => x.TenantId);

        builder.Property(x => x.WowOutcomeCode)
            .HasMaxLength(100);

        builder.Property(x => x.CurrentMRR).HasPrecision(18, 2);
        builder.Property(x => x.NetExpansionRevenue).HasPrecision(18, 2);
        builder.Property(x => x.NetContractionRevenue).HasPrecision(18, 2);
        builder.Property(x => x.LifetimeRevenue).HasPrecision(18, 2);

        builder.Property(x => x.ChurnRisk).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.ChurnType).HasConversion<string>().HasMaxLength(50);

        builder.HasOne<Tenant>()
            .WithOne()
            .HasForeignKey<TenantGrowthMetrics>(x => x.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
