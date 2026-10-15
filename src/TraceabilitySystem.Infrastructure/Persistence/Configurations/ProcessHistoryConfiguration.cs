using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TraceabilitySystem.Domain.Entities;

namespace TraceabilitySystem.Infrastructure.Persistence.Configurations;

public class ProcessHistoryConfiguration : IEntityTypeConfiguration<ProcessHistory>
{
    public void Configure(EntityTypeBuilder<ProcessHistory> builder)
    {
        builder.ToTable("process_histories");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ReferenceType)
            .HasMaxLength(50)
            .IsRequired()
            .HasDefaultValue(ProcessHistoryReferenceTypes.ProcessLog);

        builder.Property(x => x.ReferenceCode)
            .HasMaxLength(100);

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
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);

        builder.HasOne(x => x.TraceabilityLog)
            .WithMany(x => x.Histories)
            .HasForeignKey(x => x.TraceabilityLogId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);

        builder.HasOne(x => x.SerialNumber)
            .WithMany(x => x.ProcessHistories)
            .HasForeignKey(x => x.SerialNumberId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasOne(x => x.Process)
            .WithMany()
            .HasForeignKey(x => x.ProcessId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Parameter)
            .WithMany()
            .HasForeignKey(x => x.ParameterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.ReferenceType);
        builder.HasIndex(x => x.ReferenceId);
        builder.HasIndex(x => x.ReferenceCode);
        builder.HasIndex(x => x.TraceabilityLogId);
        builder.HasIndex(x => x.ProcessLogId);
        builder.HasIndex(x => x.SerialNumberId);
        builder.HasIndex(x => x.ProcessId);
        builder.HasIndex(x => x.ParameterId);
        builder.HasIndex(x => x.CreatedAt);
    }
}
