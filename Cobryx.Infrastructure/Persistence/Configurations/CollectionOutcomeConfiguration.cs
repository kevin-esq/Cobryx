using Cobryx.Domain.Collections;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cobryx.Infrastructure.Persistence.Configurations;

public class CollectionOutcomeConfiguration : IEntityTypeConfiguration<CollectionOutcome>
{
    public void Configure(EntityTypeBuilder<CollectionOutcome> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.ActionId).IsRequired();
        builder.Property(x => x.ActionType).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.AmountRecovered).HasColumnType("numeric(18,4)");
        builder.Property(x => x.DaysToRecover).IsRequired();
        builder.Property(x => x.DaysPastDueAtAction).IsRequired();
        builder.Property(x => x.WasSuccessful).IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.DaysPastDueAtAction });
    }
}
