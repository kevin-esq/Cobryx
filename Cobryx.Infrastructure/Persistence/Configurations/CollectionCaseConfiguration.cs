using Cobryx.Domain.Collections;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cobryx.Infrastructure.Persistence.Configurations;

public class CollectionCaseConfiguration : IEntityTypeConfiguration<CollectionCase>
{
    public void Configure(EntityTypeBuilder<CollectionCase> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.LoanId).IsRequired();
        builder.Property(x => x.Stage).IsRequired();
        builder.Property(x => x.DaysPastDue).IsRequired();
        builder.Property(x => x.Outstanding).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.IsClosed).IsRequired();
        builder.Property(x => x.PriorityScore).IsRequired();

        builder.HasIndex(x => x.LoanId).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.IsClosed, x.PriorityScore });
    }
}
