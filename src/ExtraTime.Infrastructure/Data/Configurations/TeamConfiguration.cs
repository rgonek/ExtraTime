using ExtraTime.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExtraTime.Infrastructure.Data.Configurations;

public sealed class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.ToTable("Teams");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.ExternalId)
            .IsRequired();

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.ShortName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(t => t.Tla)
            .HasMaxLength(5);

        builder.Property(t => t.LogoUrl)
            .HasMaxLength(500);

        builder.Property(t => t.ClubColors)
            .HasMaxLength(100);

        builder.Property(t => t.Venue)
            .HasMaxLength(200);

        builder.HasIndex(t => t.ExternalId).IsUnique();

        builder.HasMany(t => t.CompetitionTeams)
            .WithOne(ct => ct.Team)
            .HasForeignKey(ct => ct.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.HomeMatches)
            .WithOne(m => m.HomeTeam)
            .HasForeignKey(m => m.HomeTeamId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(t => t.AwayMatches)
            .WithOne(m => m.AwayTeam)
            .HasForeignKey(m => m.AwayTeamId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
