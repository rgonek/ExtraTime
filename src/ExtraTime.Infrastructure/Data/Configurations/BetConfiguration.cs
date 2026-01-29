using ExtraTime.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExtraTime.Infrastructure.Data.Configurations;

public sealed class BetConfiguration : IEntityTypeConfiguration<Bet>
{
    public void Configure(EntityTypeBuilder<Bet> builder)
    {
        builder.ToTable("Bets");

        builder.HasKey(b => b.Id);

        builder.HasOne(b => b.League)
            .WithMany()
            .HasForeignKey(b => b.LeagueId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(b => b.User)
            .WithMany()
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Match)
            .WithMany()
            .HasForeignKey(b => b.MatchId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique constraint: one bet per user per match per league
        builder.HasIndex(b => new { b.LeagueId, b.UserId, b.MatchId })
            .IsUnique();

        // Performance indexes
        builder.HasIndex(b => new { b.LeagueId, b.MatchId });
        builder.HasIndex(b => new { b.UserId, b.LeagueId });
    }
}
