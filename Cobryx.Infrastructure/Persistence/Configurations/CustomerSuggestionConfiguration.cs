using Cobryx.Domain.Identity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cobryx.Infrastructure.Persistence.Configurations;

public class CustomerSuggestionConfiguration : IEntityTypeConfiguration<CustomerSuggestion>
{
    public void Configure(EntityTypeBuilder<CustomerSuggestion> builder)
    {
        builder.HasKey(s => s.Id);
        builder.HasIndex(s => s.TenantId);
        builder.Property(s => s.Title).IsRequired().HasMaxLength(200);
        builder.Property(s => s.Description).IsRequired().HasMaxLength(2000);
        builder.Property(s => s.Status).HasMaxLength(50);
    }
}
