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
    private readonly IFootballDataApi _footballDataApi;
    private readonly ILogger<FootballDataService> _logger;
    private readonly IOptions<FootballDataSettings> _settings;
    private readonly FootballDataService _service;
    private readonly CancellationToken _ct = CancellationToken.None;

    public FootballDataServiceTests()
    {
        _footballDataApi = Substitute.For<IFootballDataApi>();
        _logger = Substitute.For<ILogger<FootballDataService>>();
        _settings = Options.Create(new FootballDataSettings
        {
            ApiKey = "test-api-key",
            BaseUrl = "https://api.football-data.org/v4",
            SupportedCompetitionIds = [2021, 2014]
        });
        _service = new FootballDataService(_footballDataApi, _settings, _logger);
    }

    [Test]
    public async Task GetCompetitionAsync_ReturnsMappedCompetition()
    {
        var competitionDto = new CompetitionApiDto(
            2021,
            "Premier League",
            "PL",
            "LEAGUE",
            new AreaApiDto("England"),
            new CurrentSeasonApiDto(555, 15, new DateTime(2024, 8, 1), new DateTime(2025, 5, 30)),
            "https://example.com/emblem.png");

        _footballDataApi.GetCompetitionAsync(2021, _ct).Returns(competitionDto);

        var result = await _service.GetCompetitionAsync(2021, _ct);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Id).IsEqualTo(2021);
        await Assert.That(result.Code).IsEqualTo("PL");
    }

    [Test]
    public async Task GetCompetitionAsync_HttpError_ReturnsNull()
    {
        _footballDataApi.GetCompetitionAsync(999, _ct)
            .Returns(Task.FromException<CompetitionApiDto>(new HttpRequestException("Connection failed")));

        var result = await _service.GetCompetitionAsync(999, _ct);

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task GetTeamsForCompetitionAsync_ReturnsMappedTeams()
    {
        var response = new TeamsApiResponse(
        [
            new TeamApiDto(1, "Arsenal", "Arsenal", "ARS", "https://example.com/arsenal.png", "Red / White", "Emirates Stadium"),
            new TeamApiDto(2, "Chelsea", "Chelsea", "CHE", "https://example.com/chelsea.png", "Blue", "Stamford Bridge")
        ]);
        _footballDataApi.GetTeamsForCompetitionAsync(2021, ct: _ct).Returns(response);

        var result = await _service.GetTeamsForCompetitionAsync(2021, _ct);

        await Assert.That(result.Count).IsEqualTo(2);
        await Assert.That(result[0].Name).IsEqualTo("Arsenal");
    }

    [Test]
    public async Task GetTeamsForCompetitionAsync_WithFilter_PassesSeasonFilter()
    {
        var response = new TeamsApiResponse([]);
        var filter = new CompetitionTeamsApiFilter(Season: 2025);
        _footballDataApi.GetTeamsForCompetitionAsync(2021, season: 2025, ct: _ct).Returns(response);

        _ = await _service.GetTeamsForCompetitionAsync(2021, filter, _ct);

        await _footballDataApi.Received(1).GetTeamsForCompetitionAsync(2021, season: 2025, ct: _ct);
    }

    [Test]
    public async Task GetMatchesForCompetitionAsync_PassesDateFilters()
    {
        var response = new MatchesApiResponse([]);
        var dateFrom = new DateTime(2026, 2, 1);
        var dateTo = new DateTime(2026, 2, 28);
        _footballDataApi.GetMatchesForCompetitionAsync(
            2021,
            null,
            null,
            null,
            dateFrom,
            dateTo,
            null,
            null,
            _ct).Returns(response);

        _ = await _service.GetMatchesForCompetitionAsync(2021, dateFrom, dateTo, _ct);

        await _footballDataApi.Received(1).GetMatchesForCompetitionAsync(
            2021,
            null,
            null,
            null,
            dateFrom,
            dateTo,
            null,
            null,
            _ct);
    }

    [Test]
    public async Task GetMatchesForCompetitionAsync_TaskCanceled_ReturnsEmptyList()
    {
        _footballDataApi.GetMatchesForCompetitionAsync(
            2021,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            _ct)
            .Returns(Task.FromException<MatchesApiResponse>(new TaskCanceledException("Timed out")));

        var result = await _service.GetMatchesForCompetitionAsync(2021, null, null, _ct);

        await Assert.That(result.Count).IsEqualTo(0);
    }

    [Test]
    public async Task GetMatchesForCompetitionAsync_WithFilter_PassesAllFilters()
    {
        var response = new MatchesApiResponse([]);
        var dateFrom = new DateTime(2026, 2, 1);
        var dateTo = new DateTime(2026, 2, 28);
        var filter = new CompetitionMatchesApiFilter(
            Season: 2025,
            Matchday: 23,
            Status: "FINISHED",
            DateFrom: dateFrom,
            DateTo: dateTo,
            Stage: "REGULAR_SEASON",
            Group: "GROUP_A");

        _footballDataApi.GetMatchesForCompetitionAsync(
            2021,
            2025,
            23,
            "FINISHED",
            dateFrom,
            dateTo,
            "REGULAR_SEASON",
            "GROUP_A",
            _ct).Returns(response);

        _ = await _service.GetMatchesForCompetitionAsync(2021, filter, _ct);

        await _footballDataApi.Received(1).GetMatchesForCompetitionAsync(
            2021,
            2025,
            23,
            "FINISHED",
            dateFrom,
            dateTo,
            "REGULAR_SEASON",
            "GROUP_A",
            _ct);
    }

    [Test]
    public async Task GetLiveMatchesAsync_UsesExpandedStatusFilter()
    {
        var response = new MatchesApiResponse([]);
        _footballDataApi.GetMatchesAsync(
            "IN_PLAY,PAUSED,EXTRA_TIME,PENALTY_SHOOTOUT",
            "2021,2014",
            _ct).Returns(response);

        _ = await _service.GetLiveMatchesAsync(_ct);

        await _footballDataApi.Received(1).GetMatchesAsync(
            "IN_PLAY,PAUSED,EXTRA_TIME,PENALTY_SHOOTOUT",
            "2021,2014",
            _ct);
    }

    [Test]
    public async Task GetStandingsAsync_ReturnsResponse()
    {
        var standings = new StandingsApiResponse(
            new StandingsCompetitionApiDto(2021, "Premier League", "PL"),
            new SeasonApiDto(555, new DateTime(2024, 8, 1), new DateTime(2025, 5, 30), 10, null),
            []);
        _footballDataApi.GetStandingsAsync(2021, null, null, null, _ct).Returns(standings);

        var result = await _service.GetStandingsAsync(2021, _ct);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Competition.Id).IsEqualTo(2021);
    }

    [Test]
    public async Task GetStandingsAsync_WithFilter_PassesAllFilters()
    {
        var standings = new StandingsApiResponse(
            new StandingsCompetitionApiDto(2021, "Premier League", "PL"),
            new SeasonApiDto(555, new DateTime(2024, 8, 1), new DateTime(2025, 5, 30), 10, null),
            []);
        var filter = new CompetitionStandingsApiFilter(
            Season: 2025,
            Matchday: 23,
            Date: new DateTime(2026, 2, 1));

        _footballDataApi.GetStandingsAsync(2021, 2025, 23, filter.Date, _ct).Returns(standings);

        _ = await _service.GetStandingsAsync(2021, filter, _ct);

        await _footballDataApi.Received(1).GetStandingsAsync(2021, 2025, 23, filter.Date, _ct);
    }
}
