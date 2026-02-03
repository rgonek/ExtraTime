using ExtraTime.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExtraTime.Infrastructure.Data.Configurations;

public sealed class LeagueBotMemberConfiguration : IEntityTypeConfiguration<LeagueBotMember>
{
    public void Configure(EntityTypeBuilder<LeagueBotMember> builder)
    {
        builder.ToTable("LeagueBotMembers");

        builder.HasKey(lbm => lbm.Id);
        builder.Property(lbm => lbm.Id).ValueGeneratedNever();

        builder.HasOne(lbm => lbm.League)
            .WithMany(l => l.BotMembers)
            .HasForeignKey(lbm => lbm.LeagueId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(lbm => lbm.Bot)
            .WithMany()
            .HasForeignKey(lbm => lbm.BotId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(lbm => new { lbm.LeagueId, lbm.BotId })
            .IsUnique();
    }
}
