using ExtraTime.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExtraTime.Infrastructure.Data.Configurations;

public sealed class LeagueConfiguration : IEntityTypeConfiguration<League>
{
    public void Configure(EntityTypeBuilder<League> builder)
    {
        builder.ToTable("Leagues");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).ValueGeneratedNever();

        builder.Property(l => l.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(l => l.Description)
            .HasMaxLength(500);

        builder.Property(l => l.InviteCode)
            .IsRequired()
            .HasMaxLength(8);

        builder.HasIndex(l => l.InviteCode)
            .IsUnique();

        builder.HasOne(l => l.Owner)
            .WithMany()
            .HasForeignKey(l => l.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(l => l.Members)
            .WithOne(lm => lm.League)
            .HasForeignKey(lm => lm.LeagueId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(l => l.BotMembers)
            .WithOne(bm => bm.League)
            .HasForeignKey(bm => bm.LeagueId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(l => l.BotsEnabled)
            .IsRequired()
            .HasDefaultValue(false);
    }
}
