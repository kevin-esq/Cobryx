using Cobryx.Domain.Analytics.Risk;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cobryx.Infrastructure.Persistence.Configurations.Risk
{
    public class CustomerRiskSnapshotConfiguration : IEntityTypeConfiguration<CustomerRiskSnapshot>
    {
        public void Configure(EntityTypeBuilder<CustomerRiskSnapshot> builder)
        {
            _ = builder.HasKey(static x => x.Id);

            _ = builder.Property(static x => x.BehaviorScore).HasPrecision(18, 4);
            _ = builder.Property(static x => x.ProbabilityOfDefault).HasPrecision(18, 4);

            _ = builder.HasIndex(static x => new { x.CustomerId, x.RecordedAt })
                   .HasDatabaseName("idx_risk_snapshot_customer_date");
        }
    }
}
