using ExtraTime.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExtraTime.Infrastructure.Data.Configurations;

public sealed class MatchStatsConfiguration : IEntityTypeConfiguration<MatchStats>
{
    public void Configure(EntityTypeBuilder<MatchStats> builder)
    {
        builder.ToTable("MatchStats");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Referee)
            .HasMaxLength(100);

        builder.Property(s => s.DataSource)
            .HasMaxLength(50);

        builder.HasOne(s => s.Match)
            .WithOne()
            .HasForeignKey<MatchStats>(s => s.MatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => s.MatchId)
            .IsUnique();
    }
}
