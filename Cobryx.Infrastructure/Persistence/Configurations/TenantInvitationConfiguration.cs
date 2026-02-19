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

        // We allow multiple Accepted/Revoked/Expired invitations, but only one Pending.
        builder.HasIndex(x => new { x.TenantId, x.Email })
            .HasFilter("[Status] = 0") // 0 = Pending
            .IsUnique();
            
        builder.HasIndex(x => x.TokenHash)
            .IsUnique();

        builder.HasIndex(x => new { x.Status, x.ExpiresAt })
            .HasDatabaseName("IX_TenantInvitation_Status_ExpiresAt");
    }
}
