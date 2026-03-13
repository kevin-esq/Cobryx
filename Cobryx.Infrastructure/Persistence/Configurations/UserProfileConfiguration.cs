using Cobryx.Domain.Identity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cobryx.Infrastructure.Persistence.Configurations;

public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.HasKey(p => p.UserId);

        builder.Property(p => p.PhoneNumber).HasMaxLength(20);
        builder.Property(p => p.AvatarUrl).HasMaxLength(500);
        builder.Property(p => p.PreferredLanguage).IsRequired().HasMaxLength(10);
        builder.Property(p => p.Timezone).IsRequired().HasMaxLength(50);

        builder.HasOne(p => p.User)
            .WithOne(u => u.Profile)
            .HasForeignKey<UserProfile>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
