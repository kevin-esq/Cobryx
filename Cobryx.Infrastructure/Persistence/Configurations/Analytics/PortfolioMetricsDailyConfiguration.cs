using Cobryx.Domain.Analytics;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cobryx.Infrastructure.Persistence.Configurations.Analytics;

public class PortfolioMetricsDailyConfiguration : IEntityTypeConfiguration<PortfolioMetricsDaily>
{
    public void Configure(EntityTypeBuilder<PortfolioMetricsDaily> builder)
    {
        builder.ToTable("PortfolioMetricsDaily");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TotalOutstanding).HasPrecision(18, 2);
        builder.Property(x => x.TotalPrincipal).HasPrecision(18, 2);
        builder.Property(x => x.TotalInterest).HasPrecision(18, 2);
        builder.Property(x => x.TotalLateFees).HasPrecision(18, 2);

        builder.Property(x => x.NPLRatio).HasPrecision(18, 4);
        builder.Property(x => x.DelinquencyRate).HasPrecision(18, 4);
        builder.Property(x => x.CollectionEfficiency).HasPrecision(18, 4);

        builder.Property(x => x.RevenueMTD).HasPrecision(18, 2);
        builder.Property(x => x.RevenueYTD).HasPrecision(18, 2);

        builder.Property(x => x.Bucket0To30).HasPrecision(18, 2);
        builder.Property(x => x.Bucket31To60).HasPrecision(18, 2);
        builder.Property(x => x.Bucket61To90).HasPrecision(18, 2);
        builder.Property(x => x.Bucket90Plus).HasPrecision(18, 2);

        builder.HasIndex(x => new { x.TenantId, x.Date })
               .IsUnique()
               .HasDatabaseName("idx_portfolio_metrics_tenant_date");
    }
}
