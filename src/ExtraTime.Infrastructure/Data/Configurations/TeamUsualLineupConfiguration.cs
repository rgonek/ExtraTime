using ExtraTime.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExtraTime.Infrastructure.Data.Configurations;

public sealed class TeamUsualLineupConfiguration : IEntityTypeConfiguration<TeamUsualLineup>
{
    public void Configure(EntityTypeBuilder<TeamUsualLineup> builder)
    {
        builder.ToTable("TeamUsualLineups");

        builder.HasKey(tul => tul.Id);
        builder.Property(tul => tul.Id).ValueGeneratedNever();

        builder.HasOne(tul => tul.Team)
            .WithMany()
            .HasForeignKey(tul => tul.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(tul => tul.Season)
            .WithMany()
            .HasForeignKey(tul => tul.SeasonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(tul => tul.UsualFormation).HasMaxLength(20);
        builder.Property(tul => tul.CaptainName).HasMaxLength(150);
        builder.Property(tul => tul.UsualGoalkeepers).HasMaxLength(2000);
        builder.Property(tul => tul.UsualDefenders).HasMaxLength(2000);
        builder.Property(tul => tul.UsualMidfielders).HasMaxLength(2000);
        builder.Property(tul => tul.UsualForwards).HasMaxLength(2000);

        builder.HasIndex(tul => new { tul.TeamId, tul.SeasonId }).IsUnique();
    }
}
