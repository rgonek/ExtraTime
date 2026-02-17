using System.Text.Json;
using ExtraTime.Domain.Common;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using ExtraTime.Infrastructure.Data;
using ExtraTime.Infrastructure.Services.Football;
using ExtraTime.UnitTests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace ExtraTime.UnitTests.Infrastructure.Services;

public sealed class TeamUsualLineupServiceTests
{
    [After(Test)]
    public void Cleanup()
    {
        Clock.Current = null!;
    }

    [Test]
    public async Task GetOrCalculateAsync_WhenLineupsExist_ShouldCalculateUsualLineup()
    {
        // Arrange
        Clock.Current = new FakeClock(new DateTime(2026, 2, 1, 12, 0, 0, DateTimeKind.Utc));
        await using var context = CreateContext();
        var (season, homeTeam) = await SeedFinishedLineupsAsync(context);
        var logger = Substitute.For<ILogger<TeamUsualLineupService>>();
        var service = new TeamUsualLineupService(context, logger);

        // Act
        var usual = await service.GetOrCalculateAsync(homeTeam.Id, season.Id, matchesToAnalyze: 5);

        // Assert
        await Assert.That(usual.UsualFormation).IsEqualTo("4-3-3");
        await Assert.That(usual.CaptainName).IsEqualTo("Arsenal Captain");
        await Assert.That(usual.MatchesAnalyzed).IsEqualTo(2);
        await Assert.That(usual.GetGoalkeepers().Single().Appearances).IsEqualTo(2);
        await Assert.That(context.TeamUsualLineups.Count()).IsEqualTo(1);
    }

    [Test]
    public async Task GetOrCalculateAsync_WhenCachedWithinWindow_ShouldReturnCachedWithoutRecompute()
    {
        // Arrange
        var now = new DateTime(2026, 2, 1, 12, 0, 0, DateTimeKind.Utc);
        Clock.Current = new FakeClock(now);
        await using var context = CreateContext();

        var competition = Competition.Create(3000, "Premier League", "PL", "England");
        var team = Team.Create(7001, "Arsenal", "ARS");
        var season = Season.Create(9001, competition.Id, 2026, now.AddMonths(-1), now.AddMonths(8), 24);
        context.Competitions.Add(competition);
        context.Teams.Add(team);
        context.Seasons.Add(season);

        var cached = TeamUsualLineup.Create(
            team.Id,
            season.Id,
            "4-2-3-1",
            JsonSerializer.Serialize(new List<UsualPlayer> { new(1, "GK", "GK", 4) }),
            "[]",
            "[]",
            "[]",
            "Captain",
            4);
        context.TeamUsualLineups.Add(cached);
        await context.SaveChangesAsync();

        var logger = Substitute.For<ILogger<TeamUsualLineupService>>();
        var service = new TeamUsualLineupService(context, logger);

        // Act
        var result = await service.GetOrCalculateAsync(team.Id, season.Id, matchesToAnalyze: 10);

        // Assert
        await Assert.That(result.Id).IsEqualTo(cached.Id);
        await Assert.That(result.UsualFormation).IsEqualTo("4-2-3-1");
        await Assert.That(context.TeamUsualLineups.Count()).IsEqualTo(1);
    }

    private static async Task<(Season Season, Team Team)> SeedFinishedLineupsAsync(ApplicationDbContext context)
    {
        var competition = Competition.Create(2021, "Premier League", "PL", "England");
        var season = Season.Create(9901, competition.Id, 2026, DateTime.UtcNow.AddMonths(-2), DateTime.UtcNow.AddMonths(7), 25);
        var homeTeam = Team.Create(101, "Arsenal", "ARS");
        var awayTeam = Team.Create(102, "Chelsea", "CHE");

        var matchOne = Match.Create(
            4001,
            competition.Id,
            homeTeam.Id,
            awayTeam.Id,
            DateTime.UtcNow.AddDays(-7),
            MatchStatus.Finished,
            seasonId: season.Id);
        var matchTwo = Match.Create(
            4002,
            competition.Id,
            homeTeam.Id,
            awayTeam.Id,
            DateTime.UtcNow.AddDays(-3),
            MatchStatus.Finished,
            seasonId: season.Id);

        var startingXi = JsonSerializer.Serialize(new List<LineupPlayer>
        {
            new(1, "Arsenal GK", "GK", 1),
            new(2, "Arsenal DEF", "DEF", 4),
            new(3, "Arsenal MID", "MID", 8),
            new(4, "Arsenal FWD", "FWD", 9)
        });

        context.Competitions.Add(competition);
        context.Teams.AddRange(homeTeam, awayTeam);
        context.Seasons.Add(season);
        context.Matches.AddRange(matchOne, matchTwo);
        context.MatchLineups.AddRange(
            MatchLineup.Create(matchOne.Id, homeTeam.Id, "4-3-3", "Coach", startingXi, "[]", "Arsenal Captain"),
            MatchLineup.Create(matchTwo.Id, homeTeam.Id, "4-3-3", "Coach", startingXi, "[]", "Arsenal Captain"));
        await context.SaveChangesAsync();

        return (season, homeTeam);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new ApplicationDbContext(options);
    }
}
