using ExtraTime.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExtraTime.Infrastructure.Data.Configurations;

public sealed class TeamSuspensionsConfiguration : IEntityTypeConfiguration<TeamSuspensions>
{
    public void Configure(EntityTypeBuilder<TeamSuspensions> builder)
    {
        builder.ToTable("TeamSuspensions");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.SuspendedPlayerNames)
            .HasMaxLength(2000);

        builder.HasOne(t => t.Team)
            .WithOne()
            .HasForeignKey<TeamSuspensions>(t => t.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => t.TeamId)
            .IsUnique();
    }
}
