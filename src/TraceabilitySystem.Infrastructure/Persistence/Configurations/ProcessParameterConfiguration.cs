using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TraceabilitySystem.Domain.Entities;

namespace TraceabilitySystem.Infrastructure.Persistence.Configurations;

public class ProcessParameterConfiguration : IEntityTypeConfiguration<ProcessParameter>
{
    public void Configure(EntityTypeBuilder<ProcessParameter> builder)
    {
        builder.HasKey(pp => new { pp.ProcessId, pp.ParameterId });

        builder.HasOne(pp => pp.Process)
            .WithMany(p => p.ProcessParameters)
            .HasForeignKey(pp => pp.ProcessId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pp => pp.Parameter)
            .WithMany(p => p.ProcessParameters)
            .HasForeignKey(pp => pp.ParameterId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
