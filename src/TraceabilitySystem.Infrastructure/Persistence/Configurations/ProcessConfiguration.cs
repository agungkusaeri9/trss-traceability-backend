using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TraceabilitySystem.Domain.Entities;

namespace TraceabilitySystem.Infrastructure.Persistence.Configurations;

public class ProcessConfiguration : IEntityTypeConfiguration<Process>
{
    public void Configure(EntityTypeBuilder<Process> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Code).IsRequired().IsUnicode().HasMaxLength(50);
        builder.Property(p => p.Name).HasMaxLength(100).IsRequired(false);
        builder.Property(p => p.Description).HasMaxLength(255).IsRequired(false);
        builder.Property(p => p.IsActive).HasDefaultValue(true);
        
        if (!AppDbContext.IsInMemory)
        {
            builder.Property(p => p.CreatedAt)
                .HasColumnType("timestamp")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        }
        builder.Property(p => p.UpdatedAt).IsRequired(false);
    }
}
