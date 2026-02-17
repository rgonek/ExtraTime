using ExtraTime.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExtraTime.Infrastructure.Data.Configurations;

public sealed class TeamXgStatsConfiguration : IEntityTypeConfiguration<TeamXgStats>
{
    public void Configure(EntityTypeBuilder<TeamXgStats> builder)
    {
        builder.ToTable("TeamXgStats");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Season)
            .IsRequired()
            .HasMaxLength(10);

        builder.HasOne(t => t.Team)
            .WithMany()
            .HasForeignKey(t => t.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Competition)
            .WithMany()
            .HasForeignKey(t => t.CompetitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => new { t.TeamId, t.CompetitionId, t.Season })
            .IsUnique();

        builder.HasIndex(t => new { t.CompetitionId, t.Season });
    }
}
