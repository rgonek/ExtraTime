using ExtraTime.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExtraTime.Infrastructure.Data.Configurations;

public sealed class BotPredictionAccuracyConfiguration : IEntityTypeConfiguration<BotPredictionAccuracy>
{
    public void Configure(EntityTypeBuilder<BotPredictionAccuracy> builder)
    {
        builder.ToTable("BotPredictionAccuracies");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Strategy)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(b => b.ModelVersion)
            .HasMaxLength(50);

        builder.Property(b => b.PeriodType)
            .HasMaxLength(20);

        builder.Property(b => b.CalculationNotes)
            .HasMaxLength(1000);

        builder.HasOne(b => b.Bot)
            .WithMany()
            .HasForeignKey(b => b.BotId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(b => new { b.BotId, b.PeriodType, b.PeriodStart })
            .HasDatabaseName("IX_BotPredictionAccuracies_BotId_PeriodType_PeriodStart");
    }
}
