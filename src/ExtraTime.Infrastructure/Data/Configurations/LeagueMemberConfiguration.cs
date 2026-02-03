using ExtraTime.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExtraTime.Infrastructure.Data.Configurations;

public sealed class LeagueMemberConfiguration : IEntityTypeConfiguration<LeagueMember>
{
    public void Configure(EntityTypeBuilder<LeagueMember> builder)
    {
        builder.ToTable("LeagueMembers");

        builder.HasKey(lm => lm.Id);
        builder.Property(lm => lm.Id).ValueGeneratedNever();

        builder.Property(lm => lm.Role)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasOne(lm => lm.League)
            .WithMany(l => l.Members)
            .HasForeignKey(lm => lm.LeagueId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(lm => lm.User)
            .WithMany()
            .HasForeignKey(lm => lm.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique constraint: user can only be in a league once
        builder.HasIndex(lm => new { lm.LeagueId, lm.UserId })
            .IsUnique();
    }
}
