using ExtraTime.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExtraTime.Infrastructure.Data.Configurations;

public sealed class BotConfiguration : IEntityTypeConfiguration<Bot>
{
    public void Configure(EntityTypeBuilder<Bot> builder)
    {
        builder.ToTable("Bots");

        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedNever();

        builder.Property(b => b.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(b => b.AvatarUrl)
            .HasMaxLength(500);

        builder.Property(b => b.Strategy)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(b => b.Configuration)
            .HasMaxLength(2000);

        builder.HasOne(b => b.User)
            .WithOne(u => u.Bot)
            .HasForeignKey<Bot>(b => b.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(b => b.UserId)
            .IsUnique();
    }
}
