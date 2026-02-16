using ExtraTime.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExtraTime.Infrastructure.Data.Configurations;

public sealed class IntegrationStatusConfiguration : IEntityTypeConfiguration<IntegrationStatus>
{
    public void Configure(EntityTypeBuilder<IntegrationStatus> builder)
    {
        builder.ToTable("IntegrationStatuses");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.IntegrationName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.Health)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(s => s.LastErrorMessage)
            .HasMaxLength(1000);

        builder.Property(s => s.LastErrorDetails)
            .HasMaxLength(4000);

        builder.Property(s => s.DisabledReason)
            .HasMaxLength(500);

        builder.Property(s => s.DisabledBy)
            .HasMaxLength(200);

        builder.Property(s => s.IsManuallyDisabled)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(s => s.CreatedAt)
            .IsRequired();

        builder.Property(s => s.UpdatedAt)
            .IsRequired();

        builder.HasIndex(s => s.IntegrationName)
            .IsUnique();
    }
}
