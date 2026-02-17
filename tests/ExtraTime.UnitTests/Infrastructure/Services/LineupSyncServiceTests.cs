using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using ExtraTime.Infrastructure.Data;
using ExtraTime.Infrastructure.Services.Football;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace ExtraTime.UnitTests.Infrastructure.Services;

public sealed class LineupSyncServiceTests
{
    [Test]
    public async Task SyncLineupForMatchAsync_WhenProviderReturnsLineup_ShouldPersistBothTeams()
    {
        // Arrange
        await using var context = CreateContext();
        var (match, homeTeam, awayTeam) = await SeedScheduledMatchAsync(context, externalId: 5001);
        var provider = new StubLineupDataProvider(_ => CreateLineupData(homeTeam.Name, awayTeam.Name));
        var logger = Substitute.For<ILogger<LineupSyncService>>();
        var service = new LineupSyncService(context, provider, logger);

        // Act
        var synced = await service.SyncLineupForMatchAsync(match.Id);

        // Assert
        await Assert.That(synced).IsTrue();
        await Assert.That(context.MatchLineups.Count()).IsEqualTo(2);

        var homeLineup = await context.MatchLineups.SingleAsync(x => x.TeamId == homeTeam.Id);
        await Assert.That(homeLineup.Formation).IsEqualTo("4-3-3");
        await Assert.That(homeLineup.GetStartingPlayers().Count).IsEqualTo(2);
    }

    [Test]
    public async Task SyncLineupsForUpcomingMatchesAsync_WhenMatchAlreadyHasLineup_ShouldSkipMatch()
    {
        // Arrange
        await using var context = CreateContext();
        var (seededMatch, homeTeam, _) = await SeedScheduledMatchAsync(context, externalId: 6001);
        context.MatchLineups.Add(MatchLineup.Create(
            seededMatch.Id,
            homeTeam.Id,
            "4-2-3-1",
            "Existing Coach",
            "[]",
            "[]",
            null));

        var (matchToSync, _, _) = await SeedScheduledMatchAsync(context, externalId: 6002);
        await context.SaveChangesAsync();

        var provider = new StubLineupDataProvider(_ => CreateLineupData("Arsenal", "Chelsea"));
        var logger = Substitute.For<ILogger<LineupSyncService>>();
        var service = new LineupSyncService(context, provider, logger);

        // Act
        var synced = await service.SyncLineupsForUpcomingMatchesAsync(TimeSpan.FromHours(4));

        // Assert
        await Assert.That(synced).IsEqualTo(1);
        await Assert.That(provider.RequestCount).IsEqualTo(1);
        await Assert.That(provider.RequestedExternalIds).Contains(matchToSync.ExternalId);
        await Assert.That(context.MatchLineups.Count()).IsEqualTo(3);
    }

    private static async Task<(Match Match, Team HomeTeam, Team AwayTeam)> SeedScheduledMatchAsync(
        ApplicationDbContext context,
        int externalId)
    {
        var competition = Competition.Create(2021, "Premier League", "PL", "England");
        var homeTeam = Team.Create(externalId + 10, "Arsenal", "ARS");
        var awayTeam = Team.Create(externalId + 20, "Chelsea", "CHE");
        var season = Season.Create(9000 + externalId, competition.Id, 2026, DateTime.UtcNow.AddMonths(-1), DateTime.UtcNow.AddMonths(8), 25);
        var match = Match.Create(
            externalId,
            competition.Id,
            homeTeam.Id,
            awayTeam.Id,
            DateTime.UtcNow.AddHours(2),
            MatchStatus.Scheduled,
            seasonId: season.Id);

        context.Competitions.Add(competition);
        context.Teams.AddRange(homeTeam, awayTeam);
        context.Seasons.Add(season);
        context.Matches.Add(match);
        await context.SaveChangesAsync();

        return (match, homeTeam, awayTeam);
    }

    private static MatchLineupData CreateLineupData(string homeName, string awayName)
    {
        var home = new TeamLineupData(
            "4-3-3",
            $"{homeName} Coach",
            $"{homeName} Captain",
            [
                new LineupPlayerData(1, $"{homeName} GK", "GK", 1),
                new LineupPlayerData(2, $"{homeName} Mid", "MID", 8)
            ],
            []);

        var away = new TeamLineupData(
            "4-4-2",
            $"{awayName} Coach",
            $"{awayName} Captain",
            [
                new LineupPlayerData(11, $"{awayName} GK", "GK", 1),
                new LineupPlayerData(12, $"{awayName} Def", "DEF", 4)
            ],
            []);

        return new MatchLineupData(home, away);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new ApplicationDbContext(options);
    }

    private sealed class StubLineupDataProvider(
        Func<MatchLineupRequest, MatchLineupData?> responseFactory) : ILineupDataProvider
    {
        public int RequestCount { get; private set; }
        public List<int> RequestedExternalIds { get; } = [];

        public Task<MatchLineupData?> GetMatchLineupAsync(
            MatchLineupRequest request,
            CancellationToken cancellationToken = default)
        {
            RequestCount++;
            RequestedExternalIds.Add(request.MatchExternalId);
            return Task.FromResult(responseFactory(request));
        }
    }
}
