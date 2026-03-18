using Cobryx.Domain.Analytics.Risk;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cobryx.Infrastructure.Persistence.Configurations.Risk;

public class CustomerRiskSnapshotConfiguration : IEntityTypeConfiguration<CustomerRiskSnapshot>
{
    public void Configure(EntityTypeBuilder<CustomerRiskSnapshot> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ProbabilityOfDefault).HasPrecision(18, 4);

        builder.HasIndex(x => x.CustomerId);
        builder.HasIndex(x => x.RecordedAt);
    }
}
