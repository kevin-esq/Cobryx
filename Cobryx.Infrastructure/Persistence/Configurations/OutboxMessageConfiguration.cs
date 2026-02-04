using Cobryx.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cobryx.Infrastructure.Persistence.Configurations;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.Content)
            .IsRequired();

        builder.Property(x => x.ProcessedAt);
        builder.Property(x => x.Error);

        builder.HasIndex(x => x.ProcessedAt);
    }
}
