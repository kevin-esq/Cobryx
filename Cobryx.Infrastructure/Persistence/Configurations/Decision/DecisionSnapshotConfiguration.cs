using Cobryx.Domain.Decision;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cobryx.Infrastructure.Persistence.Configurations.Decision;

public class DecisionSnapshotConfiguration : IEntityTypeConfiguration<DecisionSnapshot>
{
    public void Configure(EntityTypeBuilder<DecisionSnapshot> builder)
    {
        builder.ToTable("DecisionSnapshots");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.CustomerId, x.CreatedAt })
               .HasDatabaseName("idx_decision_customer_date");
    }
}
