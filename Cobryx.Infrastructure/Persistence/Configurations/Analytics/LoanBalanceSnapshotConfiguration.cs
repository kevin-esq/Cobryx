using Cobryx.Domain.Analytics;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cobryx.Infrastructure.Persistence.Configurations.Analytics;

public class LoanBalanceSnapshotConfiguration : IEntityTypeConfiguration<LoanBalanceSnapshot>
{
    public void Configure(EntityTypeBuilder<LoanBalanceSnapshot> builder)
    {
        builder.ToTable("LoanBalanceSnapshots");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type)
               .HasConversion<string>()
               .HasMaxLength(50);

        builder.Property(x => x.PrincipalBalance).HasPrecision(18, 2);
        builder.Property(x => x.InterestBalance).HasPrecision(18, 2);
        builder.Property(x => x.LateFeeBalance).HasPrecision(18, 2);

        // Required indexes for performance of ranking window functions
        builder.HasIndex(x => new { x.LoanId, x.RecordedAt })
               .HasDatabaseName("idx_snapshot_loan_recorded");

        builder.HasIndex(x => new { x.TenantId, x.RecordedAt })
               .HasDatabaseName("idx_snapshot_tenant_recorded");

        builder.HasIndex(x => x.RecordedAt)
               .IsDescending()
               .HasDatabaseName("idx_snapshot_recent");
    }
}
