using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TraceabilitySystem.Domain.Entities;

namespace TraceabilitySystem.Infrastructure.Persistence.Configurations;

public class StockInReworkConfiguration : IEntityTypeConfiguration<StockInRework>
{
    public void Configure(EntityTypeBuilder<StockInRework> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SerialNumberId)
            .IsRequired();

        builder.Property(x => x.IssueNumberBefore)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.IssueNumberAfter)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Qty)
            .IsRequired();

        builder.Property(x => x.Note)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(x => x.Status)
            .IsRequired();

        builder.Property(x => x.Disposition)
            .HasMaxLength(20)
            .IsRequired(false);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired(false);

        // FK ke SerialNumber
        builder.HasOne(x => x.SerialNumber)
            .WithMany()
            .HasForeignKey(x => x.SerialNumberId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
