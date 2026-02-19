using Cobryx.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cobryx.Infrastructure.Persistence.Configurations;

public class TenantInvitationConfiguration : IEntityTypeConfiguration<TenantInvitation>
{
    public void Configure(EntityTypeBuilder<TenantInvitation> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.TokenHash)
            .IsRequired()
            .HasMaxLength(256);

        // CTO Adjustment: Ensure one invitation per email per tenant.
        // We allow re-inviting once the old one is no longer Pending if needed,
        // but for simplicity and strict safety, we'll start with a hard unique constraint on the pair.
        // If they want to re-invite, the system should update the existing pending one.
        builder.HasIndex(x => new { x.TenantId, x.Email })
            .IsUnique();
            
        builder.HasIndex(x => x.TokenHash)
            .IsUnique();
    }
}
