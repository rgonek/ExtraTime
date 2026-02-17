using ExtraTime.Application.Features.Bots.Services;
using ExtraTime.Infrastructure.Data;
using ExtraTime.Infrastructure.Services;
using ExtraTime.Infrastructure.Services.Football;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace ExtraTime.UnitTests.Infrastructure.Services;

public sealed class MlFeatureExtractorTests
{
    [Test]
    public async Task ExtractFeaturesAsync_WithHistoricalData_ReturnsPopulatedFeatures()
    {
        await using var context = await CreateSeededContextAsync();
        var extractor = CreateExtractor(context);
        var upcomingMatch = await context.Matches
            .AsNoTracking()
            .FirstAsync(m => m.Status == MatchStatus.Scheduled);

        var features = await extractor.ExtractFeaturesAsync(upcomingMatch.Id);

        await Assert.That(features.MatchId).IsEqualTo(upcomingMatch.Id.ToString());
        await Assert.That(features.H2HMatchesPlayed).IsGreaterThan(0f);
        await Assert.That(features.HomeOdds).IsGreaterThan(0f);
        await Assert.That(features.HomeFormPointsLast5).IsGreaterThanOrEqualTo(0f);
    }

    [Test]
    public async Task GetTrainingDataAsync_WithFinishedMatches_ReturnsTargets()
    {
        await using var context = await CreateSeededContextAsync();
        var extractor = CreateExtractor(context);

        var trainingData = await extractor.GetTrainingDataAsync();

        await Assert.That(trainingData.Count).IsGreaterThanOrEqualTo(2);
        await Assert.That(trainingData.Any(t => t.ActualHomeScore == 2 && t.ActualAwayScore == 1)).IsTrue();
        await Assert.That(trainingData.All(t => t.Features.MatchId.Length > 0)).IsTrue();
    }

    private static MlFeatureExtractor CreateExtractor(ApplicationDbContext context)
    {
        var formCalculator = new TeamFormCalculator(
            context,
            TimeProvider.System,
            Substitute.For<ILogger<TeamFormCalculator>>());
        var headToHeadService = new HeadToHeadService(
            context,
            Substitute.For<ILogger<HeadToHeadService>>());

        return new MlFeatureExtractor(
            context,
            formCalculator,
            headToHeadService,
            Substitute.For<ILogger<MlFeatureExtractor>>());
    }

    private static async Task<ApplicationDbContext> CreateSeededContextAsync()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new ApplicationDbContext(options);

        var competition = Competition.Create(2001, "Premier League", "PL", "England", type: CompetitionType.League);
        var homeTeam = Team.Create(100, "Home FC", "HOME");
        var awayTeam = Team.Create(101, "Away FC", "AWAY");

        var finishedMatchOne = Match.Create(
            5001,
            competition.Id,
            homeTeam.Id,
            awayTeam.Id,
            DateTime.UtcNow.AddDays(-14),
            MatchStatus.Finished);
        finishedMatchOne.UpdateScore(2, 1);

        var finishedMatchTwo = Match.Create(
            5002,
            competition.Id,
            awayTeam.Id,
            homeTeam.Id,
            DateTime.UtcNow.AddDays(-7),
            MatchStatus.Finished);
        finishedMatchTwo.UpdateScore(1, 1);

        var upcomingMatch = Match.Create(
            5003,
            competition.Id,
            homeTeam.Id,
            awayTeam.Id,
            DateTime.UtcNow.AddDays(2),
            MatchStatus.Scheduled);

        context.Competitions.Add(competition);
        context.Teams.AddRange(homeTeam, awayTeam);
        context.Matches.AddRange(finishedMatchOne, finishedMatchTwo, upcomingMatch);
        context.MatchOdds.Add(new MatchOdds
        {
            MatchId = upcomingMatch.Id,
            HomeWinOdds = 1.9,
            DrawOdds = 3.4,
            AwayWinOdds = 4.1,
            HomeWinProbability = 0.52,
            DrawProbability = 0.27,
            AwayWinProbability = 0.21,
            MarketFavorite = MatchOutcome.HomeWin,
            FavoriteConfidence = 0.52,
            ImportedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();
        return context;
    }
}
