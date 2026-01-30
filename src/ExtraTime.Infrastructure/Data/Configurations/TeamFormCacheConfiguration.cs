using ExtraTime.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExtraTime.Infrastructure.Data.Configurations;

public sealed class TeamFormCacheConfiguration : IEntityTypeConfiguration<TeamFormCache>
{
    public void Configure(EntityTypeBuilder<TeamFormCache> builder)
    {
        builder.ToTable("TeamFormCaches");

        builder.HasKey(t => t.Id);

        builder.HasOne(t => t.Team)
            .WithMany()
            .HasForeignKey(t => t.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Competition)
            .WithMany()
            .HasForeignKey(t => t.CompetitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(t => t.RecentForm)
            .HasMaxLength(20);

        builder.HasIndex(t => new { t.TeamId, t.CompetitionId })
            .IsUnique();

        builder.HasIndex(t => t.CalculatedAt);
    }
}
