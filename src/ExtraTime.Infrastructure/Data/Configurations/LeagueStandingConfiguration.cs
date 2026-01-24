using ExtraTime.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExtraTime.Infrastructure.Data.Configurations;

public sealed class LeagueStandingConfiguration : IEntityTypeConfiguration<LeagueStanding>
{
    public void Configure(EntityTypeBuilder<LeagueStanding> builder)
    {
        builder.ToTable("league_standings");

        builder.HasKey(ls => ls.Id);

        builder.HasOne(ls => ls.League)
            .WithMany()
            .HasForeignKey(ls => ls.LeagueId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ls => ls.User)
            .WithMany()
            .HasForeignKey(ls => ls.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique constraint: one standing per user per league
        builder.HasIndex(ls => new { ls.LeagueId, ls.UserId })
            .IsUnique();

        // Performance index for leaderboard queries
        // Includes UserId to support the full ORDER BY clause: TotalPoints DESC, ExactMatches DESC, BetsPlaced ASC, UserId ASC
        builder.HasIndex(ls => new { ls.LeagueId, ls.TotalPoints, ls.ExactMatches, ls.BetsPlaced, ls.UserId });
    }
}
