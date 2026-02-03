using System.Net;
using System.Text.Json;
using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Football.DTOs;
using ExtraTime.Infrastructure.Configuration;
using ExtraTime.Infrastructure.Services.Football;
using ExtraTime.UnitTests.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace ExtraTime.UnitTests.Infrastructure.Services;

[TestCategory("Significant")]
public sealed class FootballDataServiceTests
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FootballDataService> _logger;
    private readonly IOptions<FootballDataSettings> _settings;
    private readonly FakeHttpMessageHandler _fakeHandler;
    private readonly FootballDataService _service;
    private readonly CancellationToken _ct = CancellationToken.None;

    public FootballDataServiceTests()
    {
        _fakeHandler = new FakeHttpMessageHandler();
        _httpClient = new HttpClient(_fakeHandler)
        {
            BaseAddress = new Uri("https://api.football-data.org/v4/")
        };
        _logger = Substitute.For<ILogger<FootballDataService>>();
        _settings = Options.Create(new FootballDataSettings
        {
            ApiKey = "test-api-key",
            BaseUrl = "https://api.football-data.org/v4",
            SupportedCompetitionIds = [2021, 2014]
        });
        _service = new FootballDataService(_httpClient, _settings, _logger);
    }

    [After(Test)]
    public void Cleanup()
    {
        _httpClient.Dispose();
        _fakeHandler.Dispose();
    }

    [Test]
    public async Task GetCompetitionAsync_ReturnsMappedCompetition()
    {
        // Arrange
        var competitionDto = new CompetitionApiDto(
            2021,
            "Premier League",
            "PL",
            new AreaApiDto("England"),
            new CurrentSeasonApiDto(15, new DateTime(2024, 8, 1), new DateTime(2025, 5, 30)),
            "https://example.com/emblem.png"
        );
        _fakeHandler.SetResponse("competitions/2021", JsonSerializer.Serialize(competitionDto));

        // Act
        var result = await _service.GetCompetitionAsync(2021, _ct);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Id).IsEqualTo(2021);
        await Assert.That(result.Name).IsEqualTo("Premier League");
        await Assert.That(result.Code).IsEqualTo("PL");
        await Assert.That(result.Area.Name).IsEqualTo("England");
        await Assert.That(result.CurrentSeason!.CurrentMatchday).IsEqualTo(15);
    }

    [Test]
    public async Task GetCompetitionAsync_NotFound_ReturnsNull()
    {
        // Arrange
        _fakeHandler.SetStatusCode("competitions/999", HttpStatusCode.NotFound);

        // Act
        var result = await _service.GetCompetitionAsync(999, _ct);

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task GetCompetitionAsync_ApiError_ReturnsNull()
    {
        // Arrange
        _fakeHandler.SetException("competitions/2021", new HttpRequestException("Connection failed"));

        // Act
        var result = await _service.GetCompetitionAsync(2021, _ct);

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task GetTeamsForCompetitionAsync_ReturnsMappedTeams()
    {
        // Arrange
        var teamsResponse = new TeamsApiResponse(
        [
            new TeamApiDto(1, "Arsenal", "Arsenal", "ARS", "https://example.com/arsenal.png", "Red / White", "Emirates Stadium"),
            new TeamApiDto(2, "Chelsea", "Chelsea", "CHE", "https://example.com/chelsea.png", "Blue", "Stamford Bridge")
        ]);
        _fakeHandler.SetResponse("competitions/2021/teams", JsonSerializer.Serialize(teamsResponse));

        // Act
        var result = await _service.GetTeamsForCompetitionAsync(2021, _ct);

        // Assert
        await Assert.That(result.Count).IsEqualTo(2);
        await Assert.That(result[0].Id).IsEqualTo(1);
        await Assert.That(result[0].Name).IsEqualTo("Arsenal");
        await Assert.That(result[1].Name).IsEqualTo("Chelsea");
    }

    [Test]
    public async Task GetTeamsForCompetitionAsync_ApiError_ReturnsEmptyList()
    {
        // Arrange
        _fakeHandler.SetException("competitions/2021/teams", new HttpRequestException("API Error"));

        // Act
        var result = await _service.GetTeamsForCompetitionAsync(2021, _ct);

        // Assert
        await Assert.That(result.Count).IsEqualTo(0);
    }

    [Test]
    public async Task GetMatchesForCompetitionAsync_ReturnsMappedMatches()
    {
        // Arrange
        var matchesResponse = new MatchesApiResponse(
        [
            new MatchApiDto(
                101,
                new MatchCompetitionApiDto(2021, "Premier League"),
                new MatchTeamApiDto(1, "Arsenal", "ARS", null),
                new MatchTeamApiDto(2, "Chelsea", "CHE", null),
                new DateTime(2026, 2, 1, 15, 0, 0, DateTimeKind.Utc),
                "SCHEDULED",
                25,
                "Regular Season",
                null,
                new ScoreApiDto(null, "REGULAR", new ScoreDetailApiDto(null, null), new ScoreDetailApiDto(null, null)),
                "Emirates Stadium"
            )
        ]);
        var dateFrom = new DateTime(2026, 2, 1);
        var dateTo = new DateTime(2026, 2, 28);
        _fakeHandler.SetResponse($"competitions/2021/matches?dateFrom=2026-02-01&dateTo=2026-02-28", JsonSerializer.Serialize(matchesResponse));

        // Act
        var result = await _service.GetMatchesForCompetitionAsync(2021, dateFrom, dateTo, _ct);

        // Assert
        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result[0].Id).IsEqualTo(101);
        await Assert.That(result[0].HomeTeam.Name).IsEqualTo("Arsenal");
    }

    [Test]
    public async Task GetMatchesForCompetitionAsync_ApiError_ReturnsEmptyList()
    {
        // Arrange
        _fakeHandler.SetException("competitions/2021/matches", new HttpRequestException("API Error"));

        // Act
        var result = await _service.GetMatchesForCompetitionAsync(2021, null, null, _ct);

        // Assert
        await Assert.That(result.Count).IsEqualTo(0);
    }

    [Test]
    public async Task GetMatchesForCompetitionAsync_NoDateFilters_BuildsCorrectUrl()
    {
        // Arrange
        var matchesResponse = new MatchesApiResponse([]);
        _fakeHandler.SetResponse("competitions/2021/matches", JsonSerializer.Serialize(matchesResponse));

        // Act
        var result = await _service.GetMatchesForCompetitionAsync(2021, null, null, _ct);

        // Assert
        await Assert.That(_fakeHandler.LastRequestUrl).IsEqualTo("competitions/2021/matches");
    }

    [Test]
    public async Task GetLiveMatchesAsync_ReturnsLiveMatches()
    {
        // Arrange
        var matchesResponse = new MatchesApiResponse(
        [
            new MatchApiDto(
                101,
                new MatchCompetitionApiDto(2021, "Premier League"),
                new MatchTeamApiDto(1, "Arsenal", "ARS", null),
                new MatchTeamApiDto(2, "Chelsea", "CHE", null),
                new DateTime(2026, 2, 1, 15, 0, 0, DateTimeKind.Utc),
                "IN_PLAY",
                25,
                "Regular Season",
                null,
                new ScoreApiDto("HOME", "REGULAR", new ScoreDetailApiDto(2, 1), new ScoreDetailApiDto(1, 0)),
                "Emirates Stadium"
            )
        ]);
        _fakeHandler.SetResponse("matches?status=IN_PLAY,PAUSED&competitions=2021,2014", JsonSerializer.Serialize(matchesResponse));

        // Act
        var result = await _service.GetLiveMatchesAsync(_ct);

        // Assert
        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result[0].Status).IsEqualTo("IN_PLAY");
        await Assert.That(result[0].Score.FullTime.Home).IsEqualTo(2);
        await Assert.That(result[0].Score.FullTime.Away).IsEqualTo(1);
    }

    [Test]
    public async Task GetLiveMatchesAsync_ApiError_ReturnsEmptyList()
    {
        // Arrange
        _fakeHandler.SetException("matches?status=IN_PLAY,PAUSED&competitions=2021,2014", new HttpRequestException("API Error"));

        // Act
        var result = await _service.GetLiveMatchesAsync(_ct);

        // Assert
        await Assert.That(result.Count).IsEqualTo(0);
    }

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Dictionary<string, (string Content, HttpStatusCode StatusCode)> _responses = [];
        private readonly Dictionary<string, Exception> _exceptions = [];
        public string? LastRequestUrl { get; private set; }

        public void SetResponse(string url, string content)
        {
            _responses[url] = (content, HttpStatusCode.OK);
        }

        public void SetStatusCode(string url, HttpStatusCode statusCode)
        {
            _responses[url] = ("", statusCode);
        }

        public void SetException(string url, Exception exception)
        {
            _exceptions[url] = exception;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var url = request.RequestUri!.ToString().Replace("https://api.football-data.org/v4/", "");
            LastRequestUrl = url;

            if (_exceptions.TryGetValue(url, out var exception))
            {
                throw exception;
            }

            if (_responses.TryGetValue(url, out var response))
            {
                var message = new HttpResponseMessage(response.StatusCode)
                {
                    Content = new StringContent(response.Content)
                };
                return Task.FromResult(message);
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }
}
