using ExtraTime.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExtraTime.Infrastructure.Data.Configurations;

public sealed class MatchConfiguration : IEntityTypeConfiguration<Match>
{
    public void Configure(EntityTypeBuilder<Match> builder)
    {
        builder.ToTable("Matches");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();

        builder.Property(m => m.ExternalId)
            .IsRequired();

        builder.Property(m => m.MatchDateUtc)
            .IsRequired();

        builder.Property(m => m.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(m => m.Stage)
            .HasMaxLength(50);

        builder.Property(m => m.Group)
            .HasMaxLength(50);

        builder.Property(m => m.Venue)
            .HasMaxLength(200);

        builder.HasIndex(m => m.ExternalId).IsUnique();
        builder.HasIndex(m => m.MatchDateUtc);
        builder.HasIndex(m => m.Status);
        builder.HasIndex(m => new { m.MatchDateUtc, m.Status });
    }
}
