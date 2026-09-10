using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TraceabilitySystem.Domain.Entities;

namespace TraceabilitySystem.Infrastructure.Persistence.Configurations;

public class TraceabilityLogConfiguration : IEntityTypeConfiguration<TraceabilityLog>
{
    public void Configure(EntityTypeBuilder<TraceabilityLog> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(x => x.Code);

        builder.Property(x => x.SerialNumberClinching)
            .HasMaxLength(100);

        builder.Property(x => x.SerialNumberMFan)
            .HasMaxLength(100);

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

        builder.HasMany(x => x.Details)
            .WithOne(x => x.TraceabilityLog)
            .HasForeignKey(x => x.TraceabilityLogId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
