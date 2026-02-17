using ExtraTime.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExtraTime.Infrastructure.Data.Configurations;

public sealed class PlayerSuspensionConfiguration : IEntityTypeConfiguration<PlayerSuspension>
{
    public void Configure(EntityTypeBuilder<PlayerSuspension> builder)
    {
        builder.ToTable("PlayerSuspensions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.PlayerName)
            .HasMaxLength(200);

        builder.Property(s => s.Position)
            .HasMaxLength(16);

        builder.Property(s => s.SuspensionReason)
            .HasMaxLength(100);

        builder.HasOne(s => s.Team)
            .WithMany()
            .HasForeignKey(s => s.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => new { s.TeamId, s.IsActive });
    }
}
