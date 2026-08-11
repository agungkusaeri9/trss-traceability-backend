using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TraceabilitySystem.Domain.Entities;

namespace TraceabilitySystem.Infrastructure.Persistence.Configurations;

public class ProcessCategoryConfiguration : IEntityTypeConfiguration<ProcessCategory>
{
    public void Configure(EntityTypeBuilder<ProcessCategory> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        if (!AppDbContext.IsInMemory)
        {
            builder.Property(x => x.CreatedAt)
                .HasColumnType("timestamp")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        }

        builder.Property(x => x.UpdatedAt).IsRequired(false);
    }
}
