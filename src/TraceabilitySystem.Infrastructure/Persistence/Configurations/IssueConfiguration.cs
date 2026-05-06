using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TraceabilitySystem.Domain.Entities;

namespace TraceabilitySystem.Infrastructure.Persistence.Configurations;

public class IssueConfiguration : IEntityTypeConfiguration<Issue>
{
    public void Configure(EntityTypeBuilder<Issue> builder)
    {
        builder.ToTable("issues");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Number)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(i => i.Number)
            .IsUnique();

        builder.HasOne(i => i.StockIn)
            .WithMany(s => s.Issues)
            .HasForeignKey(i => i.StockInId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
