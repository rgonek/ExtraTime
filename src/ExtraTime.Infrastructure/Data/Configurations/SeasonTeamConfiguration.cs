using ExtraTime.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExtraTime.Infrastructure.Data.Configurations;

public sealed class SeasonTeamConfiguration : IEntityTypeConfiguration<SeasonTeam>
{
    public void Configure(EntityTypeBuilder<SeasonTeam> builder)
    {
        builder.ToTable("SeasonTeams");

        builder.HasKey(st => st.Id);
        builder.Property(st => st.Id).ValueGeneratedNever();

        builder.HasIndex(st => new { st.SeasonId, st.TeamId }).IsUnique();

        builder.HasOne(st => st.Season)
            .WithMany(s => s.SeasonTeams)
            .HasForeignKey(st => st.SeasonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(st => st.Team)
            .WithMany(t => t.SeasonTeams)
            .HasForeignKey(st => st.TeamId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
