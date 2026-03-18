using Cobryx.Domain.Analytics.Risk;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cobryx.Infrastructure.Persistence.Configurations.Risk;

public class RiskEventConfiguration : IEntityTypeConfiguration<RiskEvent>
{
    public void Configure(EntityTypeBuilder<RiskEvent> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.EventType).HasMaxLength(100);
        builder.Property(x => x.ImpactScore).HasPrecision(18, 4);

        builder.HasIndex(x => x.CustomerId);
    }
}
