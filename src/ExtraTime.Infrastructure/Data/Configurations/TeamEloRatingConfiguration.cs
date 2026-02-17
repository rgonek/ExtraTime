using ExtraTime.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExtraTime.Infrastructure.Data.Configurations;

public sealed class TeamEloRatingConfiguration : IEntityTypeConfiguration<TeamEloRating>
{
    public void Configure(EntityTypeBuilder<TeamEloRating> builder)
    {
        builder.ToTable("TeamEloRatings");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.ClubEloName)
            .HasMaxLength(100);

        builder.HasOne(t => t.Team)
            .WithMany()
            .HasForeignKey(t => t.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => new { t.TeamId, t.RatingDate })
            .IsUnique();

        builder.HasIndex(t => t.RatingDate);
    }
}
