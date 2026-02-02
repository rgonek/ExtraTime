using ExtraTime.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExtraTime.Infrastructure.Data.Configurations;

public sealed class CompetitionConfiguration : IEntityTypeConfiguration<Competition>
{
    public void Configure(EntityTypeBuilder<Competition> builder)
    {
        builder.ToTable("Competitions");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.ExternalId)
            .IsRequired();

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Code)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(c => c.Country)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.LogoUrl)
            .HasMaxLength(500);

        builder.HasIndex(c => c.ExternalId).IsUnique();

        builder.HasMany(c => c.CompetitionTeams)
            .WithOne(ct => ct.Competition)
            .HasForeignKey(ct => ct.CompetitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Matches)
            .WithOne(m => m.Competition)
            .HasForeignKey(m => m.CompetitionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
