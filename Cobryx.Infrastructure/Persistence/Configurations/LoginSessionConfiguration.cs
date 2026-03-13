using Cobryx.Domain.Identity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cobryx.Infrastructure.Persistence.Configurations;

public class LoginSessionConfiguration : IEntityTypeConfiguration<LoginSession>
{
    public void Configure(EntityTypeBuilder<LoginSession> builder)
    {
        builder.HasIndex(s => s.TenantId);
        builder.HasIndex(s => s.UserId);
        builder.HasIndex(s => s.LastActiveAt);

        builder.Property(s => s.IpAddress).IsRequired().HasMaxLength(45);
        builder.Property(s => s.DeviceFingerprint).HasMaxLength(255);

        builder.HasOne(s => s.User)
            .WithMany(u => u.Sessions)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
