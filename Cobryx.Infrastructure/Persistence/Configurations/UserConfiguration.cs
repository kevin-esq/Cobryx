using Cobryx.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cobryx.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasIndex(u => u.TenantId);
        builder.HasIndex(u => new { u.TenantId, u.Email }).IsUnique();
        builder.HasIndex(u => new { u.TenantId, u.LastName, u.FirstName });

        builder.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(u => u.LastName).IsRequired().HasMaxLength(100);
        builder.Property(u => u.Email).IsRequired().HasMaxLength(255);

        builder.HasOne(u => u.Role)
            .WithMany()
            .HasForeignKey(u => u.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.OwnsMany(u => u.RefreshTokens, rt =>
        {
            rt.ToTable("RefreshTokens");
            rt.WithOwner().HasForeignKey("UserId");
            rt.HasKey(x => x.Id);
            rt.Property(x => x.Token).IsRequired().HasMaxLength(200);
            rt.Property(x => x.RowVersion).IsRowVersion();
        });
    }
}
