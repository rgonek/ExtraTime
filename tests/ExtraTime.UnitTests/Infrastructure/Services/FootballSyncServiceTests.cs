using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Football.DTOs;
using ExtraTime.Domain.Common;
using ExtraTime.Domain.Entities;
using ExtraTime.Domain.Enums;
using ExtraTime.Infrastructure.Configuration;
using ExtraTime.Infrastructure.Services.Football;
using ExtraTime.UnitTests.Attributes;
using ExtraTime.UnitTests.Common;
using ExtraTime.UnitTests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace ExtraTime.UnitTests.Infrastructure.Services;

[TestCategory("Significant")]
public sealed class FootballSyncServiceTests : HandlerTestBase
{
    private readonly IFootballDataService _footballDataService;
    private readonly IJobDispatcher _jobDispatcher;
    private readonly IOptions<FootballDataSettings> _settings;
    private readonly ILogger<FootballSyncService> _logger;
    private readonly FootballSyncService _service;
    private readonly DateTime _now = new(2026, 2, 1, 12, 0, 0, DateTimeKind.Utc);

    public FootballSyncServiceTests()
    {
        _footballDataService = Substitute.For<IFootballDataService>();
        _jobDispatcher = Substitute.For<IJobDispatcher>();
        _settings = Options.Create(new FootballDataSettings
        {
            ApiKey = "test-key",
            SupportedCompetitionIds = [2021]
        });
        _logger = Substitute.For<ILogger<FootballSyncService>>();
        _service = new FootballSyncService(Context, _footballDataService, _jobDispatcher, _settings, _logger);

        var mockSeasons = CreateMockDbSet(new List<Season>().AsQueryable());
        Context.Seasons.Returns(mockSeasons);
    }

    [After(Test)]
    public void Cleanup()
    {
        Clock.Current = null!;
    }

    [Test]
    public async Task SyncCompetitionsAsync_NewCompetition_AddsToDatabase()
    {
        // Arrange
        Clock.Current = new FakeClock(_now);
        var apiDto = new CompetitionApiDto(
            2021, "Premier League", "PL",
            "LEAGUE",
            new AreaApiDto("England"),
            new CurrentSeasonApiDto(555, 15, _now, _now.AddMonths(9)),
            "emblem.png"
        );
        _footballDataService.GetCompetitionAsync(2021, CancellationToken).Returns(apiDto);

        var mockCompetitions = CreateMockDbSet(new List<Competition>().AsQueryable());
        Context.Competitions.Returns(mockCompetitions);
        Context.SaveChangesAsync(CancellationToken).Returns(1);

        // Act
        await _service.SyncCompetitionsAsync(CancellationToken);

        // Assert
        mockCompetitions.Received(1).Add(Arg.Is<Competition>(c =>
            c.ExternalId == 2021 &&
            c.Name == "Premier League" &&
            c.Code == "PL"));
        await Context.Received(1).SaveChangesAsync(CancellationToken);
    }

    [Test]
    public async Task SyncCompetitionsAsync_ExistingCompetition_UpdatesDatabase()
    {
        // Arrange
        Clock.Current = new FakeClock(_now);
        var existingComp = Competition.Create(2021, "Old Name", "PL", "England");
        existingComp.UpdateCurrentSeason(10, _now.AddYears(-1), _now);

        var apiDto = new CompetitionApiDto(
            2021, "Premier League", "PL",
            "LEAGUE",
            new AreaApiDto("England"),
            new CurrentSeasonApiDto(555, 15, _now, _now.AddMonths(9)),
            "new-emblem.png"
        );
        _footballDataService.GetCompetitionAsync(2021, CancellationToken).Returns(apiDto);

        var mockCompetitions = CreateMockDbSet(new List<Competition> { existingComp }.AsQueryable());
        Context.Competitions.Returns(mockCompetitions);
        Context.SaveChangesAsync(CancellationToken).Returns(1);

        // Act
        await _service.SyncCompetitionsAsync(CancellationToken);

        // Assert
        await Assert.That(existingComp.Name).IsEqualTo("Premier League");
        await Assert.That(existingComp.LogoUrl).IsEqualTo("new-emblem.png");
        await Assert.That(existingComp.CurrentMatchday).IsEqualTo(15);
        await Context.Received(1).SaveChangesAsync(CancellationToken);
    }

    [Test]
    public async Task SyncCompetitionsAsync_ApiFailure_Continues()
    {
        // Arrange
        _footballDataService.GetCompetitionAsync(2021, CancellationToken).Returns((CompetitionApiDto?)null);

        var mockCompetitions = CreateMockDbSet(new List<Competition>().AsQueryable());
        Context.Competitions.Returns(mockCompetitions);

        // Act
        await _service.SyncCompetitionsAsync(CancellationToken);

        // Assert - Service continued without exception
        await Assert.That(_service).IsNotNull();
    }

    [Test]
    public async Task SyncCompetitionsAsync_DuplicateIdInSettings_OnlyAddsOnce()
    {
        // Arrange - settings with duplicate competition ID
        Clock.Current = new FakeClock(_now);
        var settingsWithDupe = Options.Create(new FootballDataSettings
        {
            ApiKey = "test-key",
            SupportedCompetitionIds = [2015, 2015] // Duplicate!
        });
        var serviceWithDupe = new FootballSyncService(Context, _footballDataService, _jobDispatcher, settingsWithDupe, _logger);

        var apiDto = new CompetitionApiDto(
            2015, "Ligue 1", "FL1",
            "LEAGUE",
            new AreaApiDto("France"),
            new CurrentSeasonApiDto(556, 20, _now, _now.AddMonths(9)),
            "ligue1.png"
        );
        _footballDataService.GetCompetitionAsync(2015, CancellationToken).Returns(apiDto);

        var competitions = new List<Competition>();
        var mockCompetitions = CreateMockDbSet(competitions.AsQueryable());
        mockCompetitions.When(x => x.Add(Arg.Any<Competition>())).Do(c => competitions.Add(c.Arg<Competition>()));
        Context.Competitions.Returns(mockCompetitions);
        Context.SaveChangesAsync(CancellationToken).Returns(1);

        // Act
        await serviceWithDupe.SyncCompetitionsAsync(CancellationToken);

        // Assert - Should only add once, not twice
        mockCompetitions.Received(1).Add(Arg.Is<Competition>(c => c.ExternalId == 2015));
    }

    [Test]
    public async Task SyncTeamsForCompetitionAsync_NewTeams_AddsToDatabase()
    {
        // Arrange
        Clock.Current = new FakeClock(_now);
        var competition = Competition.Create(2021, "Premier League", "PL", "England");
        competition.UpdateCurrentSeason(1, _now, _now.AddMonths(9));

        var apiTeams = new List<TeamApiDto>
        {
            new(1, "Arsenal", "Arsenal", "ARS", "crest.png", "Red / White", "Emirates")
        };
        _footballDataService.GetTeamsForCompetitionAsync(2021, CancellationToken).Returns(apiTeams);

        var mockCompetitions = CreateMockDbSet(new List<Competition> { competition }.AsQueryable());
        Context.Competitions.Returns(mockCompetitions);

        var mockTeams = CreateMockDbSet(new List<Team>().AsQueryable());
        Context.Teams.Returns(mockTeams);

        var season = Season.Create(555, competition.Id, _now.Year, _now, _now.AddMonths(9), 1);
        var mockSeasons = CreateMockDbSet(new List<Season> { season }.AsQueryable());
        Context.Seasons.Returns(mockSeasons);

        var mockSeasonTeams = CreateMockDbSet(new List<SeasonTeam>().AsQueryable());
        Context.SeasonTeams.Returns(mockSeasonTeams);

        Context.SaveChangesAsync(CancellationToken).Returns(1);

        // Act
        await _service.SyncTeamsForCompetitionAsync(competition.Id, CancellationToken);

        // Assert
        mockTeams.Received(1).Add(Arg.Is<Team>(t =>
            t.ExternalId == 1 &&
            t.Name == "Arsenal"));
        mockSeasonTeams.Received(1).Add(Arg.Is<SeasonTeam>(st => st.SeasonId == season.Id));
    }

    [Test]
    public async Task SyncMatchesAsync_NewMatches_AddsToDatabase()
    {
        // Arrange
        Clock.Current = new FakeClock(_now);
        var competition = Competition.Create(2021, "Premier League", "PL", "England");
        var homeTeam = Team.Create(1, "Arsenal", "ARS");
        var awayTeam = Team.Create(2, "Chelsea", "CHE");

        var apiMatches = new List<MatchApiDto>
        {
            new(
                101,
                new MatchCompetitionApiDto(2021, "Premier League"),
                new MatchTeamApiDto(1, "Arsenal", "ARS", null),
                new MatchTeamApiDto(2, "Chelsea", "CHE", null),
                _now.AddDays(1),
                "SCHEDULED",
                25,
                "Regular Season",
                null,
                new ScoreApiDto(null, "REGULAR", new ScoreDetailApiDto(null, null), new ScoreDetailApiDto(null, null)),
                "Emirates"
            )
        };
        _footballDataService.GetMatchesForCompetitionAsync(2021, Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), CancellationToken)
            .Returns(apiMatches);

        var mockCompetitions = CreateMockDbSet(new List<Competition> { competition }.AsQueryable());
        Context.Competitions.Returns(mockCompetitions);

        var mockTeams = CreateMockDbSet(new List<Team> { homeTeam, awayTeam }.AsQueryable());
        Context.Teams.Returns(mockTeams);

        var mockMatches = CreateMockDbSet(new List<Match>().AsQueryable());
        Context.Matches.Returns(mockMatches);

        Context.SaveChangesAsync(CancellationToken).Returns(1);

        // Act
        await _service.SyncMatchesAsync(_now, _now.AddDays(14), CancellationToken);

        // Assert
        mockMatches.Received(1).Add(Arg.Is<Match>(m =>
            m.ExternalId == 101 &&
            m.Status == MatchStatus.Scheduled));
    }

    [Test]
    public async Task SyncMatchesAsync_MatchFinishes_EnqueuesBetCalculation()
    {
        // Arrange
        Clock.Current = new FakeClock(_now);
        var competition = Competition.Create(2021, "Premier League", "PL", "England");
        var homeTeam = Team.Create(1, "Arsenal", "ARS");
        var awayTeam = Team.Create(2, "Chelsea", "CHE");
        var existingMatch = Match.Create(101, competition.Id, homeTeam.Id, awayTeam.Id, _now.AddHours(-2), MatchStatus.InPlay);

        var apiMatches = new List<MatchApiDto>
        {
            new(
                101,
                new MatchCompetitionApiDto(2021, "Premier League"),
                new MatchTeamApiDto(1, "Arsenal", "ARS", null),
                new MatchTeamApiDto(2, "Chelsea", "CHE", null),
                _now.AddHours(-2),
                "FINISHED",
                25,
                "Regular Season",
                null,
                new ScoreApiDto("HOME", "REGULAR", new ScoreDetailApiDto(2, 1), new ScoreDetailApiDto(1, 0)),
                "Emirates"
            )
        };
        _footballDataService.GetMatchesForCompetitionAsync(2021, Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), CancellationToken)
            .Returns(apiMatches);

        var mockCompetitions = CreateMockDbSet(new List<Competition> { competition }.AsQueryable());
        Context.Competitions.Returns(mockCompetitions);

        var mockTeams = CreateMockDbSet(new List<Team> { homeTeam, awayTeam }.AsQueryable());
        Context.Teams.Returns(mockTeams);

        var mockMatches = CreateMockDbSet(new List<Match> { existingMatch }.AsQueryable());
        Context.Matches.Returns(mockMatches);

        _jobDispatcher.EnqueueAsync("CalculateBetResults", Arg.Any<object>(), CancellationToken).Returns(Guid.NewGuid());

        // Act
        await _service.SyncMatchesAsync(_now.AddDays(-1), _now.AddDays(1), CancellationToken);

        // Assert
        await _jobDispatcher.Received(1).EnqueueAsync(
            Arg.Is("CalculateBetResults"),
            Arg.Any<object>(),
            CancellationToken);
    }

    [Test]
    public async Task SyncMatchesForCompetitionAsync_CompetitionMissing_ReturnsNoFinishedMatches()
    {
        // Arrange
        var mockCompetitions = CreateMockDbSet(new List<Competition>().AsQueryable());
        Context.Competitions.Returns(mockCompetitions);

        // Act
        var result = await _service.SyncMatchesForCompetitionAsync(2021, CancellationToken);

        // Assert
        await Assert.That(result).IsEqualTo(new MatchSyncResult(2021, false));
    }

    [Test]
    public async Task SyncMatchesForCompetitionAsync_UsesIncrementalDateWindowFilter()
    {
        // Arrange
        Clock.Current = new FakeClock(_now);
        var competition = Competition.Create(2021, "Premier League", "PL", "England");
        var mockCompetitions = CreateMockDbSet(new List<Competition> { competition }.AsQueryable());
        Context.Competitions.Returns(mockCompetitions);

        var mockTeams = CreateMockDbSet(new List<Team>().AsQueryable());
        Context.Teams.Returns(mockTeams);

        var mockMatches = CreateMockDbSet(new List<Match>().AsQueryable());
        Context.Matches.Returns(mockMatches);
        Context.SaveChangesAsync(CancellationToken).Returns(1);

        _footballDataService.GetMatchesForCompetitionAsync(
                2021,
                Arg.Any<CompetitionMatchesApiFilter>(),
                CancellationToken)
            .Returns(new List<MatchApiDto>());

        // Act
        _ = await _service.SyncMatchesForCompetitionAsync(2021, CancellationToken);

        // Assert
        await _footballDataService.Received(1).GetMatchesForCompetitionAsync(
            2021,
            Arg.Is<CompetitionMatchesApiFilter>(f =>
                f.DateFrom == _now.Date.AddDays(-2)
                && f.DateTo == _now.Date.AddDays(14)
                && f.Season == null
                && f.Matchday == null
                && f.Status == null),
            CancellationToken);
    }

    [Test]
    public async Task SyncLiveMatchResultsAsync_LiveMatch_UpdatesScore()
    {
        // Arrange
        Clock.Current = new FakeClock(_now);
        var competition = Competition.Create(2021, "Premier League", "PL", "England");
        var homeTeam = Team.Create(1, "Arsenal", "ARS");
        var awayTeam = Team.Create(2, "Chelsea", "CHE");
        var existingMatch = Match.Create(101, competition.Id, homeTeam.Id, awayTeam.Id, _now.AddHours(-1), MatchStatus.InPlay);

        var liveMatches = new List<MatchApiDto>
        {
            new(
                101,
                new MatchCompetitionApiDto(2021, "Premier League"),
                new MatchTeamApiDto(1, "Arsenal", "ARS", null),
                new MatchTeamApiDto(2, "Chelsea", "CHE", null),
                _now.AddHours(-1),
                "IN_PLAY",
                25,
                "Regular Season",
                null,
                new ScoreApiDto(null, "REGULAR", new ScoreDetailApiDto(3, 2), new ScoreDetailApiDto(2, 1)),
                "Emirates"
            )
        };
        _footballDataService.GetLiveMatchesAsync(CancellationToken).Returns(liveMatches);

        var mockMatches = CreateMockDbSet(new List<Match> { existingMatch }.AsQueryable());
        Context.Matches.Returns(mockMatches);
        Context.SaveChangesAsync(CancellationToken).Returns(1);

        // Act
        await _service.SyncLiveMatchResultsAsync(CancellationToken);

        // Assert
        await Assert.That(existingMatch.HomeScore).IsEqualTo(3);
        await Assert.That(existingMatch.AwayScore).IsEqualTo(2);
        await Assert.That(existingMatch.HomeHalfTimeScore).IsEqualTo(2);
        await Assert.That(existingMatch.AwayHalfTimeScore).IsEqualTo(1);
    }
}
