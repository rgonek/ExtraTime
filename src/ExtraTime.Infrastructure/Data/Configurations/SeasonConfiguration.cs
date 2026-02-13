using ExtraTime.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExtraTime.Infrastructure.Data.Configurations;

public sealed class SeasonConfiguration : IEntityTypeConfiguration<Season>
{
    public void Configure(EntityTypeBuilder<Season> builder)
    {
        builder.ToTable("Seasons");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.HasIndex(s => new { s.CompetitionId, s.ExternalId }).IsUnique();
        builder.HasIndex(s => new { s.CompetitionId, s.IsCurrent });

        builder.HasOne(s => s.Competition)
            .WithMany(c => c.Seasons)
            .HasForeignKey(s => s.CompetitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Winner)
            .WithMany()
            .HasForeignKey(s => s.WinnerTeamId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
