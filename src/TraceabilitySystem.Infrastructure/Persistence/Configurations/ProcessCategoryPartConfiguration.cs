using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TraceabilitySystem.Domain.Entities;

namespace TraceabilitySystem.Infrastructure.Persistence.Configurations;

public class ProcessCategoryPartConfiguration : IEntityTypeConfiguration<ProcessCategoryPart>
{
    public void Configure(EntityTypeBuilder<ProcessCategoryPart> builder)
    {
        builder.HasKey(x => x.Id);

        // Unique constraint: satu Part hanya bisa ada sekali per ProcessCategory
        builder.HasIndex(x => new { x.ProcessCategoryId, x.PartId }).IsUnique();

        builder.HasOne(x => x.ProcessCategory)
            .WithMany(x => x.ProcessCategoryParts)
            .HasForeignKey(x => x.ProcessCategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Part)
            .WithMany()
            .HasForeignKey(x => x.PartId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
