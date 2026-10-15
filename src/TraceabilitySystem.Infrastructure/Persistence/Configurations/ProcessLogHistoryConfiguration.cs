using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TraceabilitySystem.Domain.Entities;

namespace TraceabilitySystem.Infrastructure.Persistence.Configurations;

public class ProcessLogHistoryConfiguration : IEntityTypeConfiguration<ProcessLogHistory>
{
    public void Configure(EntityTypeBuilder<ProcessLogHistory> builder)
    {
        builder.HasKey(x => x.Id);

        if (!AppDbContext.IsInMemory)
        {
            builder.Property(x => x.CreatedAt)
                .HasColumnType("timestamp")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        }

        builder.Property(x => x.Value)
            .HasColumnType("longtext");

        builder.HasOne(x => x.ProcessLog)
            .WithMany(x => x.Histories)
            .HasForeignKey(x => x.ProcessLogId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.SerialNumber)
            .WithMany(x => x.ProcessLogHistories)
            .HasForeignKey(x => x.SerialNumberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Process)
            .WithMany()
            .HasForeignKey(x => x.ProcessId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Parameter)
            .WithMany()
            .HasForeignKey(x => x.ParameterId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
