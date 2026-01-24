using ExtraTime.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExtraTime.Infrastructure.Data.Configurations;

public sealed class BetResultConfiguration : IEntityTypeConfiguration<BetResult>
{
    public void Configure(EntityTypeBuilder<BetResult> builder)
    {
        builder.ToTable("bet_results");

        // Use BetId as the primary key for the one-to-one dependent
        builder.HasKey(br => br.BetId);

        builder.HasOne(br => br.Bet)
            .WithOne(b => b.Result)
            .HasForeignKey<BetResult>(br => br.BetId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
