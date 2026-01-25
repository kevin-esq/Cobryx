using Cobryx.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cobryx.Infrastructure.Persistence.Configurations;

public class CreditConfiguration : IEntityTypeConfiguration<Credit>
{
    public void Configure(EntityTypeBuilder<Credit> builder)
    {
        builder.OwnsOne(c => c.Principal, p =>
        {
            p.Property(m => m.Amount).HasPrecision(18, 2);
            p.Property(m => m.Currency).HasMaxLength(3);
        });

        builder.Property(c => c.InterestRate).HasPrecision(18, 4);
    }
}
