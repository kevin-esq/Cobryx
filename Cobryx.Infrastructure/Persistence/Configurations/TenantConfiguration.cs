using Cobryx.Domain.Entities;
using Cobryx.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cobryx.Infrastructure.Persistence.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.OwnsOne(t => t.Settings, s =>
        {
            s.Property(x => x.DefaultInterestValue).HasPrecision(18, 4);
            s.Property(x => x.PenaltyValue).HasPrecision(18, 4);
            s.Property(x => x.MinimumPaymentAmount).HasPrecision(18, 2);
        });

        builder.Property(t => t.TaxId)
            .HasConversion(t => t == null ? null : t.Value, v => v == null ? null : (TaxId)v)
            .HasMaxLength(13);

        builder.Property(t => t.Industry).HasMaxLength(100);
        builder.Property(t => t.BusinessAddress).HasMaxLength(500);
    }
}
