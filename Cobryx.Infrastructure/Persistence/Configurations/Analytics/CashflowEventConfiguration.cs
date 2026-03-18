using Cobryx.Domain.Analytics;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cobryx.Infrastructure.Persistence.Configurations.Analytics;

public class CashflowEventConfiguration : IEntityTypeConfiguration<CashflowEvent>
{
    public void Configure(EntityTypeBuilder<CashflowEvent> builder)
    {
        builder.ToTable("CashflowEvents");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Amount).HasPrecision(18, 2);

        builder.Property(x => x.Direction)
               .HasConversion<string>()
               .HasMaxLength(50);

        builder.Property(x => x.Source)
               .HasConversion<string>()
               .HasMaxLength(50);

        builder.HasIndex(x => new { x.TenantId, x.OccurredAt })
               .HasDatabaseName("idx_cashflow_tenant_occurred");
    }
}
