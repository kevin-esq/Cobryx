using Cobryx.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cobryx.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasIndex(c => c.TenantId);
        builder.HasIndex(c => c.Phone);
        
        // 🚀 Professional Indexing for Searches
        builder.HasIndex(c => new { c.LastName, c.FirstName });

        builder.Property(c => c.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(c => c.LastName).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Phone).IsRequired().HasMaxLength(20);

        // 🏠 Structured Address (2FN/3FN Optimization as Owned Types)
        builder.OwnsOne(c => c.Address, a =>
        {
            a.Property(p => p.Street).HasMaxLength(200);
            a.Property(p => p.ExtNumber).HasMaxLength(20);
            a.Property(p => p.Neighborhood).HasMaxLength(100);
            a.Property(p => p.ZipCode).HasMaxLength(10);
            a.Property(p => p.City).HasMaxLength(100);
            a.Property(p => p.State).HasMaxLength(100);
        });

        // 💳 Identity Document Segregation
        builder.OwnsOne(c => c.Document, d =>
        {
            d.Property(p => p.Type).HasMaxLength(20);
            d.Property(p => p.Value).HasMaxLength(50);
            d.HasIndex(p => p.Value); // Searchable document number
        });
    }
}
