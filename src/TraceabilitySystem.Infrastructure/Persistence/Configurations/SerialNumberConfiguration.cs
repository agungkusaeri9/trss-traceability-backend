using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TraceabilitySystem.Domain.Entities;

namespace TraceabilitySystem.Infrastructure.Persistence.Configurations;

public class SerialNumberConfiguration : IEntityTypeConfiguration<SerialNumber>
{
    public void Configure(EntityTypeBuilder<SerialNumber> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SerialNumberCode)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(x => x.SerialNumberCode)
            .IsUnique();

        builder.Property(x => x.Type)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        // SerialNumber -> SerialNumberIssue
        builder.HasMany(x => x.Issues)
            .WithOne(x => x.SerialNumber)
            .HasForeignKey(x => x.SerialNumberId)
            .OnDelete(DeleteBehavior.Cascade);

        // Parent -> Child
        builder.HasMany(x => x.ParentRelations)
            .WithOne(x => x.ParentSerialNumber)
            .HasForeignKey(x => x.ParentSerialNumberId)
            .OnDelete(DeleteBehavior.Restrict);

        // Child -> Parent
        builder.HasMany(x => x.ChildRelations)
            .WithOne(x => x.ChildSerialNumber)
            .HasForeignKey(x => x.ChildSerialNumberId)
            .OnDelete(DeleteBehavior.Restrict);

        // SerialNumber -> ProcessLogs
        builder.HasMany(x => x.ProcessLogs)
            .WithOne(x => x.SerialNumber)
            .HasForeignKey(x => x.SerialNumberId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}