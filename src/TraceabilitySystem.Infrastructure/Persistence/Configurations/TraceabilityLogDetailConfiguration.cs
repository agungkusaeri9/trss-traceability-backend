using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TraceabilitySystem.Domain.Entities;

namespace TraceabilitySystem.Infrastructure.Persistence.Configurations;

public class TraceabilityLogDetailConfiguration : IEntityTypeConfiguration<TraceabilityLogDetail>
{
    public void Configure(EntityTypeBuilder<TraceabilityLogDetail> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status)
            .HasDefaultValue(true);

        builder.Property(x => x.IsFinish)
            .HasDefaultValue(false);

        if (!AppDbContext.IsInMemory)
        {
            builder.Property(x => x.CreatedAt)
                .HasColumnType("timestamp")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        }

        builder.HasOne(x => x.TraceabilityLog)
            .WithMany(x => x.Details)
            .HasForeignKey(x => x.TraceabilityLogId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Process)
            .WithMany()
            .HasForeignKey(x => x.ProcessId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Parameter)
            .WithMany()
            .HasForeignKey(x => x.ParameterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Operator)
            .WithMany()
            .HasForeignKey(x => x.OperatorId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
