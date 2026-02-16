using System.Net;
using System.Net.Http;
using System.Text;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using ExtraTime.Infrastructure.Configuration;
using ExtraTime.Infrastructure.Data;
using ExtraTime.Infrastructure.Services.ExternalData;
using ExtraTime.UnitTests.Attributes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace ExtraTime.UnitTests.Infrastructure.Services;

[TestCategory(TestCategories.Significant)]
public sealed class InjuryServiceTests
{
    [Test]
    public async Task SyncInjuriesForUpcomingMatchesAsync_ShouldPersistFilteredInjuryData()
    {
        // Arrange
        ResetQuotaCounter();
        await using var context = CreateContext();
        await SeedUpcomingMatchAsync(context);

        var json = """
            {
              "response": [
                {
                  "player": { "id": 7, "name": "Bukayo Saka", "type": "Attacker", "reason": "Hamstring strain" },
                  "fixture": { "timestamp": 1738454400 }
                },
                {
                  "player": { "id": 4, "name": "William Saliba", "type": "Defender", "reason": "Suspension" },
                  "fixture": { "timestamp": 1738454400 }
                }
              ]
            }
            """;
        var service = CreateService(
            context,
            Options.Create(new ApiFootballSettings
            {
                Enabled = true,
                ApiKey = "test-key",
                EnableEplOnlyInjurySync = true,
                QuotaPolicy = new ExternalDataQuotaPolicy
                {
                    HardDailyLimit = 100,
                    OperationalCap = 95,
                    SafetyReserve = 0,
                    MaxInjuryCallsPerDay = 15
                }
            }),
            request => request.RequestUri!.AbsolutePath.Contains("/v3/status", StringComparison.Ordinal)
                ? CreateStatusResponse(limitDay: 100, current: 0)
                : new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                },
            out var requestCount);

        // Act
        await service.SyncInjuriesForUpcomingMatchesAsync(daysAhead: 3);

        // Assert
        await Assert.That(requestCount()).IsEqualTo(3);
        await Assert.That(context.PlayerInjuries.Count()).IsEqualTo(2);
        await Assert.That(context.TeamInjuries.Count()).IsEqualTo(2);
        await Assert.That(context.TeamInjurySnapshots.Count()).IsEqualTo(2);

        var teamInjuries = await context.TeamInjuries.FirstAsync();
        await Assert.That(teamInjuries.TotalInjured).IsEqualTo(1);
        await Assert.That(teamInjuries.FirstChoiceGkInjured).IsFalse();
        await Assert.That(teamInjuries.LastSyncedAt).IsGreaterThan(DateTime.UtcNow.AddMinutes(-5));
        await Assert.That(teamInjuries.InjuryImpactScore).IsGreaterThan(0d);
    }

    [Test]
    public async Task SyncInjuriesForUpcomingMatchesAsync_WhenQuotaReservedForLineups_ShouldSkipRequests()
    {
        // Arrange
        ResetQuotaCounter();
        await using var context = CreateContext();
        await SeedUpcomingMatchAsync(context);

        var service = CreateService(
            context,
            Options.Create(new ApiFootballSettings
            {
                Enabled = true,
                ApiKey = "test-key",
                EnableEplOnlyInjurySync = true,
                QuotaPolicy = new ExternalDataQuotaPolicy
                {
                    HardDailyLimit = 10,
                    OperationalCap = 10,
                    SafetyReserve = 10,
                    MaxInjuryCallsPerDay = 5
                }
            }),
            request => request.RequestUri!.AbsolutePath.Contains("/v3/status", StringComparison.Ordinal)
                ? CreateStatusResponse(limitDay: 10, current: 0)
                : new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""{ "response": [] }""", Encoding.UTF8, "application/json")
                },
            out var requestCount);

        // Act
        await service.SyncInjuriesForUpcomingMatchesAsync(daysAhead: 3);

        // Assert
        await Assert.That(requestCount()).IsEqualTo(1);
        await Assert.That(context.PlayerInjuries.Any()).IsFalse();
        await Assert.That(context.TeamInjuries.Any()).IsFalse();
    }

    [Test]
    public async Task GetTeamInjuriesAsOfAsync_WhenSnapshotIsAfterAsOf_ShouldReturnNull()
    {
        // Arrange
        ResetQuotaCounter();
        await using var context = CreateContext();
        var team = Team.Create(10, "Arsenal", "Arsenal");
        context.Teams.Add(team);
        context.TeamInjurySnapshots.Add(new TeamInjurySnapshot
        {
            TeamId = team.Id,
            TotalInjured = 1,
            SnapshotDateUtc = new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc)
        });
        await context.SaveChangesAsync();

        var service = CreateService(
            context,
            Options.Create(new ApiFootballSettings
            {
                Enabled = true,
                ApiKey = "test-key",
                EnableEplOnlyInjurySync = true,
                QuotaPolicy = new ExternalDataQuotaPolicy
                {
                    HardDailyLimit = 100,
                    OperationalCap = 95,
                    SafetyReserve = 10,
                    MaxInjuryCallsPerDay = 15
                }
            }),
            _ => new HttpResponseMessage(HttpStatusCode.OK),
            out _);

        // Act
        var asOf = await service.GetTeamInjuriesAsOfAsync(
            team.Id,
            new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc));

        // Assert
        await Assert.That(asOf).IsNull();
    }

    [Test]
    public async Task SyncInjuriesForUpcomingMatchesAsync_WhenEplOnlyEnabled_ShouldIgnoreNonEplMatches()
    {
        // Arrange
        ResetQuotaCounter();
        await using var context = CreateContext();
        await SeedUpcomingMatchAsync(context, competitionCode: "SA");

        var service = CreateService(
            context,
            Options.Create(new ApiFootballSettings
            {
                Enabled = true,
                ApiKey = "test-key",
                EnableEplOnlyInjurySync = true,
                QuotaPolicy = new ExternalDataQuotaPolicy
                {
                    HardDailyLimit = 100,
                    OperationalCap = 95,
                    SafetyReserve = 10,
                    MaxInjuryCallsPerDay = 15
                }
            }),
            request => request.RequestUri!.AbsolutePath.Contains("/v3/status", StringComparison.Ordinal)
                ? CreateStatusResponse(limitDay: 100, current: 0)
                : new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""{ "response": [] }""", Encoding.UTF8, "application/json")
                },
            out var requestCount);

        // Act
        await service.SyncInjuriesForUpcomingMatchesAsync(daysAhead: 3);

        // Assert
        await Assert.That(requestCount()).IsEqualTo(1);
        await Assert.That(context.TeamInjuries.Any()).IsFalse();
    }

    private static InjuryService CreateService(
        ApplicationDbContext context,
        IOptions<ApiFootballSettings> options,
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory,
        out Func<int> requestCount)
    {
        var count = 0;
        var handler = new StaticResponseHttpMessageHandler(request =>
        {
            count++;
            return responseFactory(request);
        });
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api-football-v1.p.rapidapi.com")
        };

        var clientFactory = Substitute.For<IHttpClientFactory>();
        clientFactory.CreateClient("ApiFootball").Returns(client);

        requestCount = () => count;
        var logger = Substitute.For<ILogger<InjuryService>>();
        return new InjuryService(clientFactory, context, options, logger);
    }

    private static async Task SeedUpcomingMatchAsync(
        ApplicationDbContext context,
        string competitionCode = "PL")
    {
        var competition = Competition.Create(2021, "Test League", competitionCode, "England", type: CompetitionType.League);
        var home = Team.Create(10, "Arsenal", "Arsenal");
        var away = Team.Create(20, "Chelsea", "Chelsea");
        var kickoff = DateTime.UtcNow.AddDays(1);
        var match = Match.Create(
            externalId: 1001,
            competitionId: competition.Id,
            homeTeamId: home.Id,
            awayTeamId: away.Id,
            matchDateUtc: kickoff,
            status: MatchStatus.Scheduled);

        context.Competitions.Add(competition);
        context.Teams.AddRange(home, away);
        context.Matches.Add(match);
        await context.SaveChangesAsync();
    }

    private static HttpResponseMessage CreateStatusResponse(int limitDay, int current)
    {
        var statusJson = $$"""
            {
              "response": {
                "subscription": {
                  "requests": {
                    "current": {{current}},
                    "limit_day": {{limitDay}}
                  }
                }
              }
            }
            """;

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(statusJson, Encoding.UTF8, "application/json")
        };
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static void ResetQuotaCounter()
    {
        var serviceType = typeof(InjuryService);
        serviceType
            .GetField("_dailyRequestCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .SetValue(null, 0);
        serviceType
            .GetField("_requestCounterDateUtc", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .SetValue(null, DateTime.UtcNow.Date);
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
