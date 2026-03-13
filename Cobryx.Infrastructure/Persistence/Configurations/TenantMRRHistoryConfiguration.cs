using Cobryx.Domain.Identity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cobryx.Infrastructure.Persistence.Configurations;

public class TenantMRRHistoryConfiguration : IEntityTypeConfiguration<TenantMRRHistory>
{
    public void Configure(EntityTypeBuilder<TenantMRRHistory> builder)
    {
        builder.ToTable("TenantMRRHistory");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.MRR).HasPrecision(18, 2);
        builder.Property(x => x.ChangeType).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.Reason).HasMaxLength(200);

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.RecordedAt);
    }
}
