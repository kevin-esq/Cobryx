using Cobryx.Domain.Decision;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cobryx.Infrastructure.Persistence.Configurations.Decision
{
    public class ProductionSnapshotConfiguration : IEntityTypeConfiguration<ProductionSnapshot>
    {
        public void Configure(EntityTypeBuilder<ProductionSnapshot> builder)
        {
            _ = builder.ToTable("ProductionSnapshots");

            _ = builder.HasKey(static x => x.Id);

            _ = builder.HasIndex(static x => x.HashedCustomerId);

            _ = builder.HasIndex(static x => x.EngineVersion);
            _ = builder.HasIndex(static x => x.ConfigHash);
            _ = builder.HasIndex(static x => x.TraceHash);
            _ = builder.HasIndex(static x => x.CreatedAt);
        }
    }
}
