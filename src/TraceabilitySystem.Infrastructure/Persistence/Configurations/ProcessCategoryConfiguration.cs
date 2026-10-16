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

        builder.Property(x => x.PartNumber)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("");

        builder.Property(x => x.PartNumberCustomer)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("");

        if (!AppDbContext.IsInMemory)
        {
            builder.Property(x => x.CreatedAt)
                .HasColumnType("timestamp")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        }

        builder.Property(x => x.UpdatedAt).IsRequired(false);
    }
}
