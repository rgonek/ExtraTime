using ExtraTime.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExtraTime.Infrastructure.Data.Configurations;

public sealed class TeamInjurySnapshotConfiguration : IEntityTypeConfiguration<TeamInjurySnapshot>
{
    public void Configure(EntityTypeBuilder<TeamInjurySnapshot> builder)
    {
        builder.ToTable("TeamInjurySnapshots");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.SnapshotDateUtc)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(t => t.InjuredPlayerNames)
            .HasMaxLength(2000);

        builder.HasOne(t => t.Team)
            .WithMany()
            .HasForeignKey(t => t.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => new { t.TeamId, t.SnapshotDateUtc })
            .IsUnique();
    }
}
