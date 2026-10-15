using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TraceabilitySystem.Domain.Entities;

namespace TraceabilitySystem.Infrastructure.Persistence.Configurations;

public class MqttPublisherStatusConfiguration : IEntityTypeConfiguration<MqttPublisherStatus>
{
    public void Configure(EntityTypeBuilder<MqttPublisherStatus> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ProcessCode)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.IsPublisher)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.ReferenceType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.ReferenceCode)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Reason)
            .HasMaxLength(500);

        builder.Property(x => x.Date)
            .IsRequired();

        if (!AppDbContext.IsInMemory)
        {
            builder.Property(x => x.Date)
                .HasColumnType("timestamp")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.Property(x => x.CreatedAt)
                .HasColumnType("timestamp")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        }

        builder.HasIndex(x => x.ProcessCode);
        builder.HasIndex(x => new { x.ReferenceType, x.ReferenceCode });
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.Date);
    }
}
