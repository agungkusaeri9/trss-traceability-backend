using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TraceabilitySystem.Domain.Entities;

namespace TraceabilitySystem.Infrastructure.Persistence.Configurations;

public class ProcessLogDetailConfiguration : IEntityTypeConfiguration<ProcessLogDetail>
{
    public void Configure(EntityTypeBuilder<ProcessLogDetail> builder)
    {
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.ProcessLog)
            .WithMany(x => x.Details)
            .HasForeignKey(x => x.ProcessLogId)
            .OnDelete(DeleteBehavior.Cascade);

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