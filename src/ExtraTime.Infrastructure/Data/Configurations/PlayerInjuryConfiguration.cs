using ExtraTime.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExtraTime.Infrastructure.Data.Configurations;

public sealed class PlayerInjuryConfiguration : IEntityTypeConfiguration<PlayerInjury>
{
    public void Configure(EntityTypeBuilder<PlayerInjury> builder)
    {
        builder.ToTable("PlayerInjuries");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.PlayerName)
            .HasMaxLength(200);

        builder.Property(i => i.Position)
            .HasMaxLength(16);

        builder.Property(i => i.InjuryType)
            .HasMaxLength(100);

        builder.Property(i => i.InjurySeverity)
            .HasMaxLength(50);

        builder.HasOne(i => i.Team)
            .WithMany()
            .HasForeignKey(i => i.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(i => new { i.TeamId, i.IsActive });
    }
}
