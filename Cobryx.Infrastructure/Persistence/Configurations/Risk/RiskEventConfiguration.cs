using Cobryx.Domain.Analytics.Risk;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cobryx.Infrastructure.Persistence.Configurations.Risk
{
    public class RiskEventConfiguration : IEntityTypeConfiguration<RiskEvent>
    {
        public void Configure(EntityTypeBuilder<RiskEvent> builder)
        {
            _ = builder.HasKey(static x => x.Id);

            _ = builder.Property(static x => x.EventType).HasConversion<string>().HasMaxLength(100);
            _ = builder.Property(static x => x.ImpactScore).HasPrecision(18, 4);

            _ = builder.HasIndex(static x => x.CustomerId);
        }
    }
}
