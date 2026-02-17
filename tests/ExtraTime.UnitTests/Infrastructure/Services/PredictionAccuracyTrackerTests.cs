using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using ExtraTime.Infrastructure.Data;
using ExtraTime.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace ExtraTime.UnitTests.Infrastructure.Services;

public sealed class PredictionAccuracyTrackerTests
{
    [Test]
    public async Task RecalculateAccuracyAsync_WithBotBets_PersistsAccuracyRecord()
    {
        await using var context = CreateContext();
        var data = await SeedBotBetDataAsync(context);
        var tracker = new PredictionAccuracyTracker(
            context,
            Substitute.For<ILogger<PredictionAccuracyTracker>>());

        await tracker.RecalculateAccuracyAsync(
            data.MatchDateUtc.AddDays(-1),
            data.MatchDateUtc.AddDays(1));

        var record = await context.BotPredictionAccuracies
            .AsNoTracking()
            .SingleAsync();

        await Assert.That(record.BotId).IsEqualTo(data.Bot.Id);
        await Assert.That(record.TotalPredictions).IsEqualTo(1);
        await Assert.That(record.ExactScores).IsEqualTo(1);
        await Assert.That(record.CorrectResults).IsEqualTo(1);
        await Assert.That(record.BetsWon).IsEqualTo(1);
        await Assert.That(record.AvgPointsPerBet).IsEqualTo(3d);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static async Task<(Bot Bot, DateTime MatchDateUtc)> SeedBotBetDataAsync(ApplicationDbContext context)
    {
        var owner = User.Register("owner@extratime.local", "Owner", "hash");
        var botUser = User.Register("mlbot@extratime.local", "ML Bot", "hash");
        botUser.MarkAsBot();

        var bot = Bot.Create(botUser.Id, "ML Bot", BotStrategy.MachineLearning, "🤖");

        var competition = Competition.Create(2201, "Premier League", "PL", "England", type: CompetitionType.League);
        var home = Team.Create(10, "Home FC", "HOME");
        var away = Team.Create(11, "Away FC", "AWAY");

        var matchDateUtc = DateTime.UtcNow.AddHours(-2);
        var match = Match.Create(
            8001,
            competition.Id,
            home.Id,
            away.Id,
            matchDateUtc,
            MatchStatus.Finished);
        match.UpdateScore(2, 1);

        var league = League.Create("Tracker League", owner.Id, "TRK123");
        var bet = Bet.Place(league.Id, botUser.Id, match.Id, 2, 1);
        var result = BetResult.Create(bet.Id, 3, true, true);

        context.Users.AddRange(owner, botUser);
        context.Bots.Add(bot);
        context.Competitions.Add(competition);
        context.Teams.AddRange(home, away);
        context.Matches.Add(match);
        context.Leagues.Add(league);
        context.Bets.Add(bet);
        context.BetResults.Add(result);
        await context.SaveChangesAsync();

        return (bot, matchDateUtc);
    }
}
