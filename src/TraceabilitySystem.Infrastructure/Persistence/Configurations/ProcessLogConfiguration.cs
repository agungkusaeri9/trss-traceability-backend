using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TraceabilitySystem.Domain.Entities;

namespace TraceabilitySystem.Infrastructure.Persistence.Configurations;

public class ProcessLogConfiguration : IEntityTypeConfiguration<ProcessLog>
{
    public void Configure(EntityTypeBuilder<ProcessLog> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.IsActive)
            .HasDefaultValue(true);

        if (!AppDbContext.IsInMemory)
        {
            builder.Property(x => x.CreatedAt)
                .HasColumnType("timestamp")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        }

        builder.HasOne(x => x.SerialNumber)
            .WithMany(x => x.ProcessLogs)
            .HasForeignKey(x => x.SerialNumberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Details)
            .WithOne(x => x.ProcessLog)
            .HasForeignKey(x => x.ProcessLogId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}