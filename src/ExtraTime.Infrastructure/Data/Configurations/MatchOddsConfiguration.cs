using ExtraTime.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExtraTime.Infrastructure.Data.Configurations;

public sealed class MatchOddsConfiguration : IEntityTypeConfiguration<MatchOdds>
{
    public void Configure(EntityTypeBuilder<MatchOdds> builder)
    {
        builder.ToTable("MatchOdds");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.DataSource)
            .HasMaxLength(50);

        builder.HasOne(o => o.Match)
            .WithOne()
            .HasForeignKey<MatchOdds>(o => o.MatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(o => o.MatchId)
            .IsUnique();
    }
}
