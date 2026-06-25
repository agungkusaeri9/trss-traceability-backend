using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TraceabilitySystem.Domain.Entities;

namespace TraceabilitySystem.Infrastructure.Persistence.Configurations;

public class SerialNumberRelationConfiguration : IEntityTypeConfiguration<SerialNumberRelation>
{
    public void Configure(EntityTypeBuilder<SerialNumberRelation> builder)
    {
        builder.ToTable("serial_number_relations");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(100);

        builder.HasIndex(x => new
        {
            x.ParentSerialNumberId,
            x.ChildSerialNumberId
        }).IsUnique();

        builder.HasOne(x => x.ParentSerialNumber)
            .WithMany(x => x.ParentRelations)
            .HasForeignKey(x => x.ParentSerialNumberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ChildSerialNumber)
            .WithMany(x => x.ChildRelations)
            .HasForeignKey(x => x.ChildSerialNumberId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}