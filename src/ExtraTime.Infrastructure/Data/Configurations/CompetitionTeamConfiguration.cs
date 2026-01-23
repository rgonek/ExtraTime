using ExtraTime.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExtraTime.Infrastructure.Data.Configurations;

public sealed class CompetitionTeamConfiguration : IEntityTypeConfiguration<CompetitionTeam>
{
    public void Configure(EntityTypeBuilder<CompetitionTeam> builder)
    {
        builder.ToTable("competition_teams");

        builder.HasKey(ct => ct.Id);

        builder.Property(ct => ct.Season)
            .IsRequired();

        builder.HasIndex(ct => new { ct.CompetitionId, ct.TeamId, ct.Season }).IsUnique();
    }
}
