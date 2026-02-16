using ExtraTime.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExtraTime.Infrastructure.Data.Configurations;

public sealed class TeamInjuriesConfiguration : IEntityTypeConfiguration<TeamInjuries>
{
    public void Configure(EntityTypeBuilder<TeamInjuries> builder)
    {
        builder.ToTable("TeamInjuries");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.InjuredPlayerNames)
            .HasMaxLength(2000);

        builder.HasOne(t => t.Team)
            .WithOne()
            .HasForeignKey<TeamInjuries>(t => t.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => t.TeamId)
            .IsUnique();
    }
}
