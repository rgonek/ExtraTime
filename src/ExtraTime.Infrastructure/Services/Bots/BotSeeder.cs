using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ExtraTime.Infrastructure.Services.Bots;

public sealed class BotSeeder(IApplicationDbContext context, IPasswordHasher passwordHasher)
{
    public async Task SeedDefaultBotsAsync(CancellationToken cancellationToken = default)
    {
        if (await context.Bots.AnyAsync(cancellationToken))
            return;

        var bots = new[]
        {
            CreateBot("Lucky Larry", BotStrategy.Random, "🎲"),
            CreateBot("Home Hero", BotStrategy.HomeFavorer, "🏠"),
            CreateBot("Underdog Dave", BotStrategy.UnderdogSupporter, "🐕"),
            CreateBot("Draw Dan", BotStrategy.DrawPredictor, "🤝"),
            CreateBot("Goal Gary", BotStrategy.HighScorer, "⚽"),
        };

        foreach (var (user, bot) in bots)
        {
            context.Users.Add(user);
            context.Bots.Add(bot);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private (User user, Bot bot) CreateBot(string name, BotStrategy strategy, string avatarEmoji)
    {
        var email = $"bot_{name.ToLower().Replace(" ", "_")}@extratime.local";
        var user = User.Register(email, name, passwordHasher.Hash(Guid.NewGuid().ToString()));
        user.MarkAsBot();

        var bot = new Bot
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Name = name,
            AvatarUrl = null,
            Strategy = strategy,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        return (user, bot);
    }
}
