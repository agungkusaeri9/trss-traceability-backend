using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TraceabilitySystem.Domain.Entities;

namespace TraceabilitySystem.Infrastructure.Persistence.Configurations;

public class ProcessLogConfiguration : IEntityTypeConfiguration<ProcessLog>
{
    public void Configure(EntityTypeBuilder<ProcessLog> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.IssueNo)
            .HasMaxLength(50)
            .IsRequired();
    }
}
