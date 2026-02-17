using ExtraTime.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExtraTime.Infrastructure.Data.Configurations;

public sealed class MlModelVersionConfiguration : IEntityTypeConfiguration<MlModelVersion>
{
    public void Configure(EntityTypeBuilder<MlModelVersion> builder)
    {
        builder.ToTable("MlModelVersions");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.ModelType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(m => m.Version)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(m => m.BlobPath)
            .HasMaxLength(500);

        builder.Property(m => m.TrainingDataRange)
            .HasMaxLength(100);

        builder.Property(m => m.ActivationNotes)
            .HasMaxLength(500);

        builder.Property(m => m.AlgorithmUsed)
            .HasMaxLength(100);

        builder.HasIndex(m => new { m.ModelType, m.IsActive })
            .HasDatabaseName("IX_MlModelVersions_ModelType_IsActive");

        builder.HasIndex(m => m.Version)
            .IsUnique()
            .HasDatabaseName("IX_MlModelVersions_Version");
    }
}
