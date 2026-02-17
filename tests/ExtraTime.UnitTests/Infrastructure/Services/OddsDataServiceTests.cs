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

[TestCategory(TestCategories.Significant)]
public sealed class OddsDataServiceTests
{
    [Test]
    public async Task ImportSeasonOddsAsync_ShouldPersistOddsAndMatchStats()
    {
        // Arrange
        await using var context = CreateContext();
        var match = await SeedMatchAsync(context);
        var requestPath = string.Empty;
        var csv = """
            Date,HomeTeam,AwayTeam,FTHG,FTAG,B365H,B365D,B365A,HTHG,HTAG,HS,HST,AS,AST,HC,AC,HF,AF,HY,AY,HR,AR,Referee
            17/08/2025,Arsenal,Chelsea,2,1,1.80,3.60,4.40,1,0,14,7,9,3,6,4,10,12,2,3,0,0,Michael Oliver
            """;

        var service = CreateService(
            context,
            request =>
            {
                requestPath = request.RequestUri?.AbsolutePath ?? string.Empty;
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(csv, Encoding.UTF8, "text/csv")
                };
            });

        // Act
        await service.ImportSeasonOddsAsync("PL", "2025");

        // Assert
        var odds = context.MatchOdds.Single(o => o.MatchId == match.Id);
        var stats = context.MatchStats.Single(s => s.MatchId == match.Id);

        await Assert.That(requestPath).IsEqualTo("/mmz4281/2526/E0.csv");
        await Assert.That(odds.MarketFavorite).IsEqualTo(MatchOutcome.HomeWin);
        await Assert.That(odds.HomeWinProbability + odds.DrawProbability + odds.AwayWinProbability)
            .IsEqualTo(1d)
            .Within(0.000001);
        await Assert.That(stats.HomeShots).IsEqualTo(14);
        await Assert.That(stats.Referee).IsEqualTo("Michael Oliver");
    }

    [Test]
    public async Task GetOddsForMatchAsOfAsync_ShouldExcludeFutureImportedData()
    {
        // Arrange
        await using var context = CreateContext();
        var match = await SeedMatchAsync(context);
        context.MatchOdds.Add(new MatchOdds
        {
            MatchId = match.Id,
            HomeWinOdds = 1.9,
            DrawOdds = 3.5,
            AwayWinOdds = 4.3,
            ImportedAt = new DateTime(2025, 8, 16, 12, 0, 0, DateTimeKind.Utc)
        });
        await context.SaveChangesAsync();

        var service = CreateService(context, _ => new HttpResponseMessage(HttpStatusCode.OK));

        // Act
        var beforeImport = await service.GetOddsForMatchAsOfAsync(
            match.Id,
            new DateTime(2025, 8, 15, 23, 0, 0, DateTimeKind.Utc));
        var afterImport = await service.GetOddsForMatchAsOfAsync(
            match.Id,
            new DateTime(2025, 8, 16, 13, 0, 0, DateTimeKind.Utc));

        // Assert
        await Assert.That(beforeImport).IsNull();
        await Assert.That(afterImport).IsNotNull();
        await Assert.That(afterImport!.HomeWinOdds).IsEqualTo(1.9d);
    }

    [Test]
    public async Task ImportHistoricalSeasonsAsync_WithInvalidRange_ShouldThrow()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context, _ => new HttpResponseMessage(HttpStatusCode.OK));

        // Act
        var action = () => service.ImportHistoricalSeasonsAsync("PL", 2026, 2025);

        // Assert
        await Assert.That(action).Throws<ArgumentException>();
    }

    private static OddsDataService CreateService(
        ApplicationDbContext context,
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
    {
        var handler = new StaticResponseHttpMessageHandler(responseFactory);
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://www.football-data.co.uk")
        };

        var clientFactory = Substitute.For<IHttpClientFactory>();
        clientFactory.CreateClient("FootballDataUk").Returns(client);

        var logger = Substitute.For<ILogger<OddsDataService>>();
        return new OddsDataService(clientFactory, context, logger);
    }

    private static async Task<Match> SeedMatchAsync(ApplicationDbContext context)
    {
        var competition = Competition.Create(2021, "Premier League", "PL", "England", type: CompetitionType.League);
        var home = Team.Create(10, "Arsenal", "Arsenal");
        var away = Team.Create(20, "Chelsea", "Chelsea");
        var match = Match.Create(
            externalId: 1001,
            competitionId: competition.Id,
            homeTeamId: home.Id,
            awayTeamId: away.Id,
            matchDateUtc: new DateTime(2025, 8, 17, 15, 0, 0, DateTimeKind.Utc),
            status: MatchStatus.Finished);

        context.Competitions.Add(competition);
        context.Teams.AddRange(home, away);
        context.Matches.Add(match);
        await context.SaveChangesAsync();

        return match;
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
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
