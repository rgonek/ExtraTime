using ExtraTime.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExtraTime.Infrastructure.Data.Configurations;

public sealed class MatchXgStatsConfiguration : IEntityTypeConfiguration<MatchXgStats>
{
    public void Configure(EntityTypeBuilder<MatchXgStats> builder)
    {
        builder.ToTable("MatchXgStats");

        builder.HasKey(m => m.Id);

        builder.HasOne(m => m.Match)
            .WithMany()
            .HasForeignKey(m => m.MatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(m => m.MatchId)
            .IsUnique();

        builder.HasIndex(m => m.UnderstatMatchId)
            .IsUnique();
    }
}
