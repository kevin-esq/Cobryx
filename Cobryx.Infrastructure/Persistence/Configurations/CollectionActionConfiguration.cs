using Cobryx.Domain.Collections;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cobryx.Infrastructure.Persistence.Configurations;

public class CollectionActionConfiguration : IEntityTypeConfiguration<CollectionAction>
{
    public void Configure(EntityTypeBuilder<CollectionAction> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CaseId).IsRequired();
        builder.Property(x => x.ActionType).IsRequired();
        builder.Property(x => x.ExecutedAt).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(1000);

        builder.HasIndex(x => x.CaseId);
    }
}
