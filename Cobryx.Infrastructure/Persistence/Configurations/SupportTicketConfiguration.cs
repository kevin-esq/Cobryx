using Cobryx.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cobryx.Infrastructure.Persistence.Configurations;

public class SupportTicketConfiguration : IEntityTypeConfiguration<SupportTicket>
{
    public void Configure(EntityTypeBuilder<SupportTicket> builder)
    {
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.Status);

        builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Description).IsRequired();
        builder.Property(x => x.Category).HasMaxLength(50);
        
        // InternalNotes can be long
    }
}
