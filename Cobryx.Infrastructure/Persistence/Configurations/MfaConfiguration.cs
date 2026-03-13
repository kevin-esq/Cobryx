using Cobryx.Domain.Identity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cobryx.Infrastructure.Persistence.Configurations;

public class MfaDeviceConfiguration : IEntityTypeConfiguration<MfaDevice>
{
    public void Configure(EntityTypeBuilder<MfaDevice> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.DeviceName).IsRequired().HasMaxLength(100);
        builder.Property(m => m.Secret).IsRequired();
        builder.Property(m => m.CredentialId).HasMaxLength(500);
        builder.Property(m => m.PublicKey).HasMaxLength(2000);

        builder.HasOne(m => m.User)
            .WithMany(u => u.MfaDevices)
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class RecoveryCodeConfiguration : IEntityTypeConfiguration<RecoveryCode>
{
    public void Configure(EntityTypeBuilder<RecoveryCode> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.CodeHash).IsRequired().HasMaxLength(200);

        builder.HasOne(r => r.User)
            .WithMany(u => u.RecoveryCodes)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
