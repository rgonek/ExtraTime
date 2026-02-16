using System.Net;
using System.Net.Http;
using System.Text;
using ExtraTime.Application.Common.Interfaces;
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
public sealed class EloRatingServiceTests
{
    [Test]
    public async Task SyncEloRatingsForDateAsync_ShouldPersistMatchedTopFlightTeams()
    {
        // Arrange
        await using var context = CreateContext();
        var city = Team.Create(11, "Manchester City", "Man City");
        var spurs = Team.Create(12, "Tottenham Hotspur", "Tottenham");
        var arsenal = Team.Create(13, "Arsenal", "Arsenal");
        var leeds = Team.Create(14, "Leeds United", "Leeds");
        context.Teams.AddRange(city, spurs, arsenal, leeds);
        await context.SaveChangesAsync();

        var requestPath = string.Empty;
        var csv = """
            Rank,Club,Country,Level,Elo,From,To
            1,Man City,ENG,1,1960.2,2025-08-17,2025-08-18
            2,Spurs,ENG,1,1810.5,2025-08-17,2025-08-18
            3,Arsenal,ENG,1,1888.1,2025-08-17,2025-08-18
            4,Leeds,ENG,2,1702.7,2025-08-17,2025-08-18
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
            },
            out var healthService);

        // Act
        var ratingDate = new DateTime(2025, 8, 17, 9, 0, 0, DateTimeKind.Utc);
        await service.SyncEloRatingsForDateAsync(ratingDate);

        // Assert
        await Assert.That(requestPath).IsEqualTo("/2025-08-17");
        await Assert.That(context.TeamEloRatings.Count()).IsEqualTo(3);

        var spursRating = context.TeamEloRatings.Single(r => r.TeamId == spurs.Id);
        await Assert.That(spursRating.ClubEloName).IsEqualTo("Spurs");
        await Assert.That(spursRating.EloRank).IsEqualTo(2);

        await Assert.That(context.TeamEloRatings.Any(r => r.TeamId == leeds.Id)).IsFalse();
        await healthService.Received(1).RecordSuccessAsync(
            IntegrationType.ClubElo,
            Arg.Any<TimeSpan>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetTeamEloAtDateAsync_ShouldExcludeFutureRatings()
    {
        // Arrange
        await using var context = CreateContext();
        var team = Team.Create(10, "Arsenal", "Arsenal");
        context.Teams.Add(team);
        context.TeamEloRatings.AddRange(
            new TeamEloRating
            {
                TeamId = team.Id,
                EloRating = 1820,
                EloRank = 12,
                ClubEloName = "Arsenal",
                RatingDate = new DateTime(2025, 8, 10, 0, 0, 0, DateTimeKind.Utc),
                SyncedAt = new DateTime(2025, 8, 10, 1, 0, 0, DateTimeKind.Utc)
            },
            new TeamEloRating
            {
                TeamId = team.Id,
                EloRating = 1850,
                EloRank = 8,
                ClubEloName = "Arsenal",
                RatingDate = new DateTime(2025, 8, 20, 0, 0, 0, DateTimeKind.Utc),
                SyncedAt = new DateTime(2025, 8, 20, 1, 0, 0, DateTimeKind.Utc)
            });
        await context.SaveChangesAsync();

        var service = CreateService(
            context,
            _ => new HttpResponseMessage(HttpStatusCode.OK),
            out _);

        // Act
        var asOf = await service.GetTeamEloAtDateAsync(
            team.Id,
            new DateTime(2025, 8, 15, 12, 0, 0, DateTimeKind.Utc));

        // Assert
        await Assert.That(asOf).IsNotNull();
        await Assert.That(asOf!.EloRating).IsEqualTo(1820d);
        await Assert.That(asOf.RatingDate).IsEqualTo(new DateTime(2025, 8, 10, 0, 0, 0, DateTimeKind.Utc));
    }

    [Test]
    public async Task BackfillEloRatingsAsync_WithInvalidRange_ShouldThrow()
    {
        // Arrange
        await using var context = CreateContext();
        var service = CreateService(context, _ => new HttpResponseMessage(HttpStatusCode.OK), out _);

        // Act
        var action = () => service.BackfillEloRatingsAsync(
            new DateTime(2025, 8, 20, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 8, 10, 0, 0, 0, DateTimeKind.Utc));

        // Assert
        await Assert.That(action).Throws<ArgumentException>();
    }

    private static EloRatingService CreateService(
        ApplicationDbContext context,
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory,
        out IIntegrationHealthService healthService)
    {
        var handler = new StaticResponseHttpMessageHandler(responseFactory);
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://api.clubelo.com")
        };

        var clientFactory = Substitute.For<IHttpClientFactory>();
        clientFactory.CreateClient("ClubElo").Returns(client);

        healthService = Substitute.For<IIntegrationHealthService>();
        var logger = Substitute.For<ILogger<EloRatingService>>();
        return new EloRatingService(clientFactory, context, healthService, logger);
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
