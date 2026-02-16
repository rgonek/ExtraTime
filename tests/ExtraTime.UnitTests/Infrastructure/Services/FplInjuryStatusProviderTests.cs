using System.Net;
using System.Net.Http;
using System.Text;
using ExtraTime.Infrastructure.Services.ExternalData;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace ExtraTime.UnitTests.Infrastructure.Services;

public sealed class FplInjuryStatusProviderTests
{
    [Test]
    public async Task GetCurrentStatusesAsync_WhenPayloadValid_ShouldReturnMappedPlayers()
    {
        // Arrange
        var json = """
            {
              "elements": [
                {
                  "id": 101,
                  "team": 1,
                  "web_name": "Saka",
                  "status": "d",
                  "news": "Hamstring issue",
                  "news_added": "2026-02-16T12:00:00Z"
                }
              ]
            }
            """;
        var provider = CreateProvider(
            _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        // Act
        var statuses = await provider.GetCurrentStatusesAsync();

        // Assert
        await Assert.That(statuses.Count).IsEqualTo(1);
        await Assert.That(statuses[0].PlayerName).IsEqualTo("Saka");
        await Assert.That(statuses[0].Status).IsEqualTo("d");
        await Assert.That(statuses[0].FplTeamId).IsEqualTo(1);
    }

    [Test]
    public async Task GetCurrentStatusesAsync_WhenResponseFails_ShouldReturnEmpty()
    {
        // Arrange
        var provider = CreateProvider(_ => new HttpResponseMessage(HttpStatusCode.BadGateway));

        // Act
        var statuses = await provider.GetCurrentStatusesAsync();

        // Assert
        await Assert.That(statuses).IsEmpty();
    }

    private static FplInjuryStatusProvider CreateProvider(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
    {
        var handler = new StaticResponseHttpMessageHandler(responseFactory);
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://fantasy.premierleague.com")
        };
        var clientFactory = Substitute.For<IHttpClientFactory>();
        clientFactory.CreateClient("Fpl").Returns(client);
        var logger = Substitute.For<ILogger<FplInjuryStatusProvider>>();

        return new FplInjuryStatusProvider(clientFactory, logger);
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
