using Cobryx.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cobryx.Infrastructure.Persistence.Configurations;

public class TenantSubscriptionConfiguration : IEntityTypeConfiguration<TenantSubscription>
{
    public void Configure(EntityTypeBuilder<TenantSubscription> builder)
    {
        builder.HasKey(s => s.Id);
        builder.HasIndex(s => s.TenantId);
        builder.HasOne(s => s.Plan).WithMany().HasForeignKey(s => s.PlanId);
        builder.Property(s => s.CancellationFeedback).HasMaxLength(2000);
    }
}
