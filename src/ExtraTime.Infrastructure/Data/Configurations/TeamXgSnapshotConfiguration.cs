using ExtraTime.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExtraTime.Infrastructure.Data.Configurations;

public sealed class TeamXgSnapshotConfiguration : IEntityTypeConfiguration<TeamXgSnapshot>
{
    public void Configure(EntityTypeBuilder<TeamXgSnapshot> builder)
    {
        builder.ToTable("TeamXgSnapshots");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Season)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(t => t.SnapshotDateUtc)
            .HasColumnType("date")
            .IsRequired();

        builder.HasOne(t => t.Team)
            .WithMany()
            .HasForeignKey(t => t.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Competition)
            .WithMany()
            .HasForeignKey(t => t.CompetitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => new { t.TeamId, t.CompetitionId, t.Season, t.SnapshotDateUtc })
            .IsUnique();

        builder.HasIndex(t => new { t.CompetitionId, t.Season, t.SnapshotDateUtc });
    }
}
