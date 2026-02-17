using ExtraTime.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExtraTime.Infrastructure.Data.Configurations;

public sealed class HeadToHeadConfiguration : IEntityTypeConfiguration<HeadToHead>
{
    public void Configure(EntityTypeBuilder<HeadToHead> builder)
    {
        builder.ToTable("HeadToHeads");

        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).ValueGeneratedNever();

        builder.HasOne(h => h.Team1)
            .WithMany()
            .HasForeignKey(h => h.Team1Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(h => h.Team2)
            .WithMany()
            .HasForeignKey(h => h.Team2Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(h => h.Competition)
            .WithMany()
            .HasForeignKey(h => h.CompetitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(h => new { h.Team1Id, h.Team2Id, h.CompetitionId })
            .IsUnique()
            .HasFilter(null);
    }
}
