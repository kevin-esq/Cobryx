using Cobryx.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cobryx.Infrastructure.Persistence.Configurations;

public class CreditConfiguration : IEntityTypeConfiguration<Credit>
{
    public void Configure(EntityTypeBuilder<Credit> builder)
    {
        builder.HasIndex(c => c.TenantId);
        builder.HasIndex(c => c.CustomerId);
        builder.HasIndex(c => c.Status);
        builder.HasIndex(c => c.StartDate);
        builder.HasIndex(c => new { c.TenantId, c.Status });
        builder.OwnsOne(c => c.Principal, m =>
        {
            m.Property(m => m.Amount).HasPrecision(18, 2);
            m.Property(m => m.Currency).HasMaxLength(3);
        });

        builder.Property(c => c.InterestRate).HasPrecision(18, 4);
    }
}
