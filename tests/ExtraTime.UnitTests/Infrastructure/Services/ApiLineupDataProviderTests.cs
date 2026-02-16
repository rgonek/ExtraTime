using System.Net;
using System.Net.Http;
using System.Text;
using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Infrastructure.Configuration;
using ExtraTime.Infrastructure.Services.Football;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace ExtraTime.UnitTests.Infrastructure.Services;

public sealed class ApiLineupDataProviderTests
{
    [Test]
    public async Task GetMatchLineupAsync_WhenResponseIsValid_ShouldMapLineupData()
    {
        // Arrange
        var json = """
            {
              "response": [
                {
                  "team": { "name": "Arsenal" },
                  "coach": { "name": "Mikel Arteta" },
                  "formation": "4-3-3",
                  "startXI": [
                    { "player": { "id": 1, "name": "David Raya", "number": 22, "pos": "G", "grid": "1:1", "captain": false } },
                    { "player": { "id": 2, "name": "Martin Odegaard", "number": 8, "pos": "M", "grid": "3:1", "captain": true } }
                  ],
                  "substitutes": [
                    { "player": { "id": 3, "name": "Leandro Trossard", "number": 19, "pos": "F", "grid": "4:2" } }
                  ]
                },
                {
                  "team": { "name": "Chelsea" },
                  "coach": { "name": "Enzo Maresca" },
                  "formation": "4-2-3-1",
                  "startXI": [
                    { "player": { "id": 10, "name": "Robert Sanchez", "number": 1, "pos": "GOALKEEPER", "grid": "1:1", "captain": false } }
                  ],
                  "substitutes": []
                }
              ]
            }
            """;
        var provider = CreateProvider(
            Options.Create(new ApiFootballSettings
            {
                Enabled = true,
                ApiKey = "test-key"
            }),
            _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            },
            out _);
        var request = new MatchLineupRequest(1001, "Arsenal", "Chelsea", DateTime.UtcNow, "PL");

        // Act
        var lineup = await provider.GetMatchLineupAsync(request);

        // Assert
        await Assert.That(lineup).IsNotNull();
        await Assert.That(lineup!.HomeTeam.Formation).IsEqualTo("4-3-3");
        await Assert.That(lineup.HomeTeam.CaptainName).IsEqualTo("Martin Odegaard");
        await Assert.That(lineup.HomeTeam.StartingXi.Count).IsEqualTo(2);
        await Assert.That(lineup.HomeTeam.StartingXi[0].Position).IsEqualTo("GK");
        await Assert.That(lineup.HomeTeam.StartingXi[1].Position).IsEqualTo("MID");
        await Assert.That(lineup.AwayTeam.Formation).IsEqualTo("4-2-3-1");
    }

    [Test]
    public async Task GetMatchLineupAsync_WhenApiKeyMissing_ShouldReturnNullWithoutHttpCall()
    {
        // Arrange
        var provider = CreateProvider(
            Options.Create(new ApiFootballSettings
            {
                Enabled = true,
                ApiKey = string.Empty
            }),
            _ => new HttpResponseMessage(HttpStatusCode.OK),
            out var requestCount);
        var request = new MatchLineupRequest(1001, "Arsenal", "Chelsea", DateTime.UtcNow, "PL");

        // Act
        var lineup = await provider.GetMatchLineupAsync(request);

        // Assert
        await Assert.That(lineup).IsNull();
        await Assert.That(requestCount()).IsEqualTo(0);
    }

    private static ApiLineupDataProvider CreateProvider(
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
        var logger = Substitute.For<ILogger<ApiLineupDataProvider>>();

        requestCount = () => count;
        return new ApiLineupDataProvider(clientFactory, options, logger);
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
