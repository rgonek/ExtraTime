using ExtraTime.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExtraTime.Infrastructure.Data.Configurations;

public sealed class FootballStandingConfiguration : IEntityTypeConfiguration<FootballStanding>
{
    public void Configure(EntityTypeBuilder<FootballStanding> builder)
    {
        builder.ToTable("FootballStandings");

        builder.HasKey(fs => fs.Id);
        builder.Property(fs => fs.Id).ValueGeneratedNever();

        builder.Property(fs => fs.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.Property(fs => fs.Stage).HasMaxLength(50);
        builder.Property(fs => fs.Group).HasMaxLength(20);
        builder.Property(fs => fs.Form).HasMaxLength(50);

        builder.HasIndex(fs => new { fs.SeasonId, fs.TeamId, fs.Type, fs.Stage, fs.Group }).IsUnique();
        builder.HasIndex(fs => new { fs.SeasonId, fs.Type, fs.Position });

        builder.HasOne(fs => fs.Season)
            .WithMany(s => s.Standings)
            .HasForeignKey(fs => fs.SeasonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(fs => fs.Team)
            .WithMany(t => t.FootballStandings)
            .HasForeignKey(fs => fs.TeamId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
