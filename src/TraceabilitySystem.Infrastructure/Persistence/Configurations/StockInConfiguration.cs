using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TraceabilitySystem.Domain.Entities;

namespace TraceabilitySystem.Infrastructure.Persistence.Configurations;

public class StockInConfiguration : IEntityTypeConfiguration<StockIn>
{
    public void Configure(EntityTypeBuilder<StockIn> builder)
    {
        builder.ToTable("stock_ins");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(s => s.Code)
            .IsUnique();

        builder.Property(s => s.SupplyQty)
            .IsRequired();

        builder.Property(s => s.SupplyDate)
            .IsRequired();

        builder.Property(s => s.ReceiptQty)
            .IsRequired();

        builder.Property(s => s.ReceiptDate)
            .IsRequired();

        builder.HasOne(s => s.Part)
            .WithMany()
            .HasForeignKey(s => s.PartId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
