using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TraceabilitySystem.Domain.Entities;

namespace TraceabilitySystem.Infrastructure.Persistence.Configurations
{
    internal class PrintHistoryConfiguration : IEntityTypeConfiguration<PrintHistory>
    {
        public void Configure(EntityTypeBuilder<PrintHistory> builder)
        {
            builder.ToTable("PrintHistories");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .UseMySqlIdentityColumn();

            builder.Property(x => x.Module)
                .IsRequired();

            builder.Property(x => x.ReferenceId)
                .IsRequired();

            builder.Property(x => x.ReferenceNumber)
                .HasMaxLength(100);

            builder.Property(x => x.PrinterName)
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(x => x.Status)
                .IsRequired();

            builder.Property(x => x.ErrorMessage);

            builder.Property(x => x.StackTrace);

            builder.Property(x => x.RetryCount)
                .HasDefaultValue(0);

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.Property(x => x.LastRetryAt);

            builder.HasIndex(x => new
            {
                x.Module,
                x.ReferenceId
            });

            builder.HasIndex(x => x.Status);

            builder.HasIndex(x => x.CreatedAt);
        }
    }
}