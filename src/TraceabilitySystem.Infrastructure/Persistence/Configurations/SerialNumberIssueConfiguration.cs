using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TraceabilitySystem.Domain.Entities;

namespace TraceabilitySystem.Infrastructure.Persistence.Configurations;

public class SerialNumberIssueConfiguration : IEntityTypeConfiguration<SerialNumberIssue>
{
    public void Configure(EntityTypeBuilder<SerialNumberIssue> builder)
    {
        builder.ToTable("serial_number_issues");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(100);

        builder.HasIndex(x => new
        {
            x.SerialNumberId,
            x.IssueId
        }).IsUnique();

        builder.HasOne(x => x.SerialNumber)
            .WithMany(x => x.Issues)
            .HasForeignKey(x => x.SerialNumberId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Issue)
            .WithMany(x => x.SerialNumberIssues)
            .HasForeignKey(x => x.IssueId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}