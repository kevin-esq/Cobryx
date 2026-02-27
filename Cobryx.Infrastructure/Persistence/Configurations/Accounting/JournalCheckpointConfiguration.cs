using Cobryx.Domain.Entities.Accounting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cobryx.Infrastructure.Persistence.Configurations.Accounting;

public class JournalCheckpointConfiguration : IEntityTypeConfiguration<JournalCheckpoint>
{
    public void Configure(EntityTypeBuilder<JournalCheckpoint> builder)
    {
        builder.ToTable("JournalCheckpoints");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.TenantId).IsUnique();
    }
}
