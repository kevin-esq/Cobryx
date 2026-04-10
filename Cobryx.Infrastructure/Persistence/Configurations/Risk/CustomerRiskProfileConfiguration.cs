using Cobryx.Domain.Analytics.Risk;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cobryx.Infrastructure.Persistence.Configurations.Risk;

public class CustomerRiskProfileConfiguration : IEntityTypeConfiguration<CustomerRiskProfile>
{
    public void Configure(EntityTypeBuilder<CustomerRiskProfile> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.RiskScore).HasPrecision(18, 4);
        builder.Property(x => x.BehaviorScore).HasPrecision(18, 4);
        builder.Property(x => x.CreditScore).HasPrecision(18, 4);
        builder.Property(x => x.ProbabilityOfDefault).HasPrecision(18, 4);

        builder.HasIndex(x => x.CustomerId).IsUnique();
    }
}
