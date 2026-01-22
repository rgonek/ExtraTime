using ExtraTime.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExtraTime.Infrastructure.Data.Configurations;

public sealed class BackgroundJobConfiguration : IEntityTypeConfiguration<BackgroundJob>
{
    public void Configure(EntityTypeBuilder<BackgroundJob> builder)
    {
        builder.ToTable("background_jobs");

        builder.HasKey(j => j.Id);

        builder.Property(j => j.JobType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(j => j.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(j => j.Payload)
            .HasColumnType("jsonb");

        builder.Property(j => j.Result)
            .HasColumnType("jsonb");

        builder.Property(j => j.Error)
            .HasMaxLength(4000);

        builder.Property(j => j.CorrelationId)
            .HasMaxLength(100);

        builder.HasIndex(j => j.Status);
        builder.HasIndex(j => j.JobType);
        builder.HasIndex(j => j.CreatedAt);
        builder.HasIndex(j => j.ScheduledAt);
        builder.HasIndex(j => j.CorrelationId);
    }
}
