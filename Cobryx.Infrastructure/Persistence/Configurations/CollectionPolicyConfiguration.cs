using Cobryx.Domain.Collections;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cobryx.Infrastructure.Persistence.Configurations;

public class CollectionPolicyConfiguration : IEntityTypeConfiguration<CollectionPolicy>
{
    public void Configure(EntityTypeBuilder<CollectionPolicy> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.ReminderDays).IsRequired();
        builder.Property(x => x.CallDays).IsRequired();
        builder.Property(x => x.EscalationDays).IsRequired();
        builder.Property(x => x.LegalDays).IsRequired();

        builder.HasIndex(x => x.TenantId).IsUnique();
    }
}
