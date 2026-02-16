using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using ExtraTime.Domain.ValueObjects;
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
            CreateBot("Lucky Larry", BotStrategy.Random, null, "🎲"),
            CreateBot("Home Hero", BotStrategy.HomeFavorer, null, "🏠"),
            CreateBot("Underdog Dave", BotStrategy.UnderdogSupporter, null, "🐕"),
            CreateBot("Draw Dan", BotStrategy.DrawPredictor, null, "🤝"),
            CreateBot("Goal Gary", BotStrategy.HighScorer, null, "⚽"),
            CreateBot("Stats Genius", BotStrategy.StatsAnalyst, StatsAnalystConfig.Balanced.ToJson(), "🧠"),
            CreateBot("Form Master", BotStrategy.StatsAnalyst, StatsAnalystConfig.FormFocused.ToJson(), "📈"),
            CreateBot("Fortress Fred", BotStrategy.StatsAnalyst, StatsAnalystConfig.HomeAdvantage.ToJson(), "🏰"),
            CreateBot("Goal Hunter", BotStrategy.StatsAnalyst, StatsAnalystConfig.GoalFocused.ToJson(), "🎯"),
            CreateBot("Safe Steve", BotStrategy.StatsAnalyst, StatsAnalystConfig.Conservative.ToJson(), "🛡️"),
            CreateBot("Chaos Carl", BotStrategy.StatsAnalyst, StatsAnalystConfig.Chaotic.ToJson(), "🌪️"),
            CreateBot("Data Scientist", BotStrategy.StatsAnalyst, StatsAnalystConfig.FullAnalysis.ToJson(), "🧪"),
            CreateBot("xG Expert", BotStrategy.StatsAnalyst, StatsAnalystConfig.XgFocused.ToJson(), "📊"),
            CreateBot("Market Follower", BotStrategy.StatsAnalyst, StatsAnalystConfig.MarketFollower.ToJson(), "💰"),
            CreateBot("Injury Tracker", BotStrategy.StatsAnalyst, StatsAnalystConfig.InjuryAware.ToJson(), "🏥"),
        };

        foreach (var (user, bot) in bots)
        {
            context.Users.Add(user);
            context.Bots.Add(bot);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private (User user, Bot bot) CreateBot(string name, BotStrategy strategy, string? configuration, string avatarEmoji)
    {
        var email = $"bot_{name.ToLowerInvariant().Replace(" ", "_")}@extratime.local";
        var user = User.Register(email, name, passwordHasher.Hash(Guid.NewGuid().ToString()));
        user.MarkAsBot();

        var bot = Bot.Create(
            user.Id,
            name,
            strategy,
            avatarEmoji,
            configuration);

        return (user, bot);
    }
}
