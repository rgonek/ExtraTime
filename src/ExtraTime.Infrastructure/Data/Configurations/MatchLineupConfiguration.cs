using ExtraTime.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExtraTime.Infrastructure.Data.Configurations;

public sealed class MatchLineupConfiguration : IEntityTypeConfiguration<MatchLineup>
{
    public void Configure(EntityTypeBuilder<MatchLineup> builder)
    {
        builder.ToTable("MatchLineups");

        builder.HasKey(ml => ml.Id);
        builder.Property(ml => ml.Id).ValueGeneratedNever();

        builder.HasOne(ml => ml.Match)
            .WithMany(m => m.Lineups)
            .HasForeignKey(ml => ml.MatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ml => ml.Team)
            .WithMany()
            .HasForeignKey(ml => ml.TeamId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(ml => ml.Formation).HasMaxLength(20);
        builder.Property(ml => ml.CoachName).HasMaxLength(150);
        builder.Property(ml => ml.CaptainName).HasMaxLength(150);
        builder.Property(ml => ml.StartingXi).HasMaxLength(4000);
        builder.Property(ml => ml.Bench).HasMaxLength(4000);

        builder.HasIndex(ml => new { ml.MatchId, ml.TeamId }).IsUnique();
    }
}
