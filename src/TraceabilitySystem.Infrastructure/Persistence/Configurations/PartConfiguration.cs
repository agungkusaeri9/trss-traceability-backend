using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TraceabilitySystem.Domain.Entities;

namespace TraceabilitySystem.Infrastructure.Persistence.Configurations;

public class PartConfiguration : IEntityTypeConfiguration<Part> 
{
    public void Configure(EntityTypeBuilder<Part> builder)
    {
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Number).IsRequired().IsUnicode().HasMaxLength(50);
        builder.Property(u => u.Name).IsRequired().IsUnicode().HasMaxLength(50);
        builder.Property(u => u.Description).HasMaxLength(255).IsRequired(false);
        builder.Property(u => u.IsActive).HasDefaultValue(true);
        if (!AppDbContext.IsInMemory)
        {
            builder.Property(u => u.CreatedAt)
                .HasColumnType("timestamp")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        }
        builder.Property(u => u.UpdatedAt).IsRequired(false);
    }
    
}