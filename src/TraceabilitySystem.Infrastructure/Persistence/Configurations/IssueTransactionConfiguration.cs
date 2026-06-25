using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TraceabilitySystem.Domain.Entities;

namespace TraceabilitySystem.Infrastructure.Persistence.Configurations;

public class IssueTransactionConfiguration : IEntityTypeConfiguration<IssueTransaction>
{
    public void Configure(EntityTypeBuilder<IssueTransaction> builder)
    {
        builder.ToTable("issue_transactions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.QtyBefore)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.QtyChange)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.QtyAfter)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.Type)
            .HasMaxLength(50)
            .IsRequired(false);

        builder.Property(x => x.Remark)
            .HasMaxLength(255)
            .IsRequired(false);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasOne(x => x.Issue)
            .WithMany(x => x.Transactions)
            .HasForeignKey(x => x.IssueId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}