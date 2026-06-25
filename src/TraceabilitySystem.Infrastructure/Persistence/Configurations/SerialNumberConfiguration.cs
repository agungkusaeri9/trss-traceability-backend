using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TraceabilitySystem.Domain.Entities;

namespace TraceabilitySystem.Infrastructure.Persistence.Configurations;

public class SerialNumberConfiguration : IEntityTypeConfiguration<SerialNumber>
{
    public void Configure(EntityTypeBuilder<SerialNumber> builder)
    {
        builder.ToTable("serial_numbers");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SerialNumberCode)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(x => x.SerialNumberCode)
            .IsUnique();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(100);

        builder.Property(x => x.UpdatedBy)
            .HasMaxLength(100);

        builder.Property(x => x.Type)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasMany(x => x.Issues)
            .WithOne(x => x.SerialNumber)
            .HasForeignKey(x => x.SerialNumberId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.ParentRelations)
            .WithOne(x => x.ParentSerialNumber)
            .HasForeignKey(x => x.ParentSerialNumberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.ChildRelations)
            .WithOne(x => x.ChildSerialNumber)
            .HasForeignKey(x => x.ChildSerialNumberId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}