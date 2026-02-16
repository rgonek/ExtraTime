using System.Net;
using System.Net.Http;
using System.Text;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using ExtraTime.Infrastructure.Data;
using ExtraTime.Infrastructure.Services.ExternalData;
using ExtraTime.UnitTests.Attributes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace ExtraTime.UnitTests.Infrastructure.Services;

[TestCategory("Significant")]
public sealed class UnderstatServiceTests
{
    [Test]
    public async Task SyncLeagueXgStatsAsync_ShouldPersistTeamStats()
    {
        // Arrange
        await using var context = CreateContext();
        var competition = Competition.Create(2021, "Premier League", "PL", "England", type: CompetitionType.League);
        var arsenal = Team.Create(10, "Arsenal", "Arsenal");
        var spurs = Team.Create(20, "Tottenham Hotspur", "Tottenham");

        context.Competitions.Add(competition);
        context.Teams.AddRange(arsenal, spurs);
        context.CompetitionTeams.AddRange(
            new CompetitionTeam { CompetitionId = competition.Id, TeamId = arsenal.Id, Team = arsenal, Competition = competition, Season = 2025 },
            new CompetitionTeam { CompetitionId = competition.Id, TeamId = spurs.Id, Team = spurs, Competition = competition, Season = 2025 });
        await context.SaveChangesAsync();

        var html = CreateUnderstatHtml("""
            {"1":{"title":"Arsenal","history":[{"xG":"1.4","xGA":"0.8","scored":"2","missed":"1"},{"xG":"1.0","xGA":"0.7","scored":"1","missed":"0"}]},
             "2":{"title":"Tottenham","history":[{"xG":"1.8","xGA":"1.2","scored":"3","missed":"2"},{"xG":"1.1","xGA":"1.0","scored":"1","missed":"1"}]}}
            """);

        var clientFactory = Substitute.For<IHttpClientFactory>();
        clientFactory.CreateClient("Understat").Returns(CreateHttpClient(html));

        var logger = Substitute.For<ILogger<UnderstatService>>();
        var service = new UnderstatService(clientFactory, context, logger);

        // Act
        var synced = await service.SyncLeagueXgStatsAsync("PL", "2025");

        // Assert
        await Assert.That(synced.Count).IsEqualTo(2);
        await Assert.That(context.TeamXgStats.Count()).IsEqualTo(2);

        var arsenalStats = context.TeamXgStats.Single(x => x.TeamId == arsenal.Id);
        await Assert.That(arsenalStats.XgFor).IsEqualTo(2.4);
        await Assert.That(arsenalStats.GoalsScored).IsEqualTo(3);
        await Assert.That(arsenalStats.MatchesPlayed).IsEqualTo(2);
    }

    [Test]
    public async Task GetTeamXgAsOfAsync_ShouldReturnLatestSnapshotWithoutFutureData()
    {
        // Arrange
        await using var context = CreateContext();
        var team = Team.Create(10, "Arsenal", "Arsenal");
        var competition = Competition.Create(2021, "Premier League", "PL", "England", type: CompetitionType.League);

        context.Teams.Add(team);
        context.Competitions.Add(competition);
        context.TeamXgStats.AddRange(
            new TeamXgStats
            {
                TeamId = team.Id,
                CompetitionId = competition.Id,
                Season = "2024",
                LastSyncedAt = new DateTime(2025, 5, 10, 12, 0, 0, DateTimeKind.Utc),
                XgFor = 30
            },
            new TeamXgStats
            {
                TeamId = team.Id,
                CompetitionId = competition.Id,
                Season = "2025",
                LastSyncedAt = new DateTime(2025, 9, 1, 12, 0, 0, DateTimeKind.Utc),
                XgFor = 35
            });
        await context.SaveChangesAsync();

        var clientFactory = Substitute.For<IHttpClientFactory>();
        var logger = Substitute.For<ILogger<UnderstatService>>();
        var service = new UnderstatService(clientFactory, context, logger);

        // Act
        var asOf = await service.GetTeamXgAsOfAsync(
            team.Id,
            competition.Id,
            new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc));

        // Assert
        await Assert.That(asOf).IsNotNull();
        await Assert.That(asOf!.Season).IsEqualTo("2024");
        await Assert.That(asOf.XgFor).IsEqualTo(30d);
    }

    [Test]
    public async Task SyncLeagueSeasonRangeAsync_WhenRangeIsInvalid_ShouldThrow()
    {
        // Arrange
        await using var context = CreateContext();
        var clientFactory = Substitute.For<IHttpClientFactory>();
        var logger = Substitute.For<ILogger<UnderstatService>>();
        var service = new UnderstatService(clientFactory, context, logger);

        // Act
        var action = () => service.SyncLeagueSeasonRangeAsync("PL", 2026, 2025);

        // Assert
        await Assert.That(action).Throws<ArgumentException>();
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static HttpClient CreateHttpClient(string html)
    {
        var handler = new StaticResponseHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(html, Encoding.UTF8, "text/html")
        });

        return new HttpClient(handler)
        {
            BaseAddress = new Uri("https://understat.com")
        };
    }

    private static string CreateUnderstatHtml(string payload)
    {
        var compactPayload = payload.Replace("\r", string.Empty).Replace("\n", string.Empty);
        var escapedPayload = compactPayload.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("'", "\\'", StringComparison.Ordinal);

        return $"<html><body><script>var teamsData = JSON.parse('{escapedPayload}');</script></body></html>";
    }

    private sealed class StaticResponseHttpMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(responseFactory(request));
        }
    }
}
