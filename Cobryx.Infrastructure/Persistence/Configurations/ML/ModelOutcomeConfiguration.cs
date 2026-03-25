using Cobryx.Domain.ML;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cobryx.Infrastructure.Persistence.Configurations.ML;

public class ModelOutcomeConfiguration : IEntityTypeConfiguration<ModelOutcome>
{
    public void Configure(EntityTypeBuilder<ModelOutcome> builder)
    {
        builder.ToTable("ModelOutcomes");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.CustomerId, x.CreatedAt })
               .HasDatabaseName("idx_model_outcome_customer_date");
    }
}
