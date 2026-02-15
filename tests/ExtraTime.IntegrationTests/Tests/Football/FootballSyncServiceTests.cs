using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Application.Features.Football.DTOs;
using ExtraTime.Infrastructure.Configuration;
using ExtraTime.Infrastructure.Services.Football;
using ExtraTime.IntegrationTests.Base;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace ExtraTime.IntegrationTests.Tests.Football;

public sealed class FootballSyncServiceTests : IntegrationTestBase
{
    private IFootballDataService _footballDataService = null!;
    private IJobDispatcher _jobDispatcher = null!;
    private ILogger<FootballSyncService> _logger = null!;

    [Before(Test)]
    public Task SetupSyncTests()
    {
        _footballDataService = Substitute.For<IFootballDataService>();
        _jobDispatcher = Substitute.For<IJobDispatcher>();
        _logger = Substitute.For<ILogger<FootballSyncService>>();
        return Task.CompletedTask;
    }

    [Test]
    public async Task SyncCompetitionsAsync_DuplicateIdInSettings_OnlyInsertsOnce()
    {
        // Arrange - settings with duplicate competition ID (simulating config issue)
        var settingsWithDupe = Options.Create(new FootballDataSettings
        {
            ApiKey = "test-key",
            SupportedCompetitionIds = [2015, 2015, 2015] // Triplicate!
        });

        var apiDto = new CompetitionApiDto(
            2015, "Ligue 1", "FL1",
            "LEAGUE",
            new AreaApiDto("France"),
            new CurrentSeasonApiDto(556, 20, DateTime.UtcNow, DateTime.UtcNow.AddMonths(9)),
            "ligue1.png"
        );
        _footballDataService.GetCompetitionAsync(2015, Arg.Any<CancellationToken>()).Returns(apiDto);

        var service = new FootballSyncService(Context, _footballDataService, _jobDispatcher, settingsWithDupe, _logger);

        // Act - should NOT throw DbUpdateException for duplicate key
        await service.SyncCompetitionsAsync();

        // Assert - should have exactly one competition in DB
        var competitions = Context.Competitions.Where(c => c.ExternalId == 2015).ToList();
        await Assert.That(competitions.Count).IsEqualTo(1);
        await Assert.That(competitions[0].Name).IsEqualTo("Ligue 1");
    }

    [Test]
    public async Task SyncCompetitionsAsync_MultipleUniqueCompetitions_InsertsAll()
    {
        // Arrange
        var settings = Options.Create(new FootballDataSettings
        {
            ApiKey = "test-key",
            SupportedCompetitionIds = [2021, 2014, 2015]
        });

        _footballDataService.GetCompetitionAsync(2021, Arg.Any<CancellationToken>()).Returns(
            new CompetitionApiDto(2021, "Premier League", "PL", "LEAGUE",
                new AreaApiDto("England"), null, "pl.png"));
        _footballDataService.GetCompetitionAsync(2014, Arg.Any<CancellationToken>()).Returns(
            new CompetitionApiDto(2014, "La Liga", "PD", "LEAGUE",
                new AreaApiDto("Spain"), null, "laliga.png"));
        _footballDataService.GetCompetitionAsync(2015, Arg.Any<CancellationToken>()).Returns(
            new CompetitionApiDto(2015, "Ligue 1", "FL1", "LEAGUE",
                new AreaApiDto("France"), null, "ligue1.png"));

        var service = new FootballSyncService(Context, _footballDataService, _jobDispatcher, settings, _logger);

        // Act
        await service.SyncCompetitionsAsync();

        // Assert
        var competitions = Context.Competitions.ToList();
        await Assert.That(competitions.Count).IsEqualTo(3);
    }

    [Test]
    public async Task SyncCompetitionsAsync_RunTwice_UpdatesExistingCompetitions()
    {
        // Arrange
        var settings = Options.Create(new FootballDataSettings
        {
            ApiKey = "test-key",
            SupportedCompetitionIds = [2015]
        });

        _footballDataService.GetCompetitionAsync(2015, Arg.Any<CancellationToken>()).Returns(
            new CompetitionApiDto(2015, "Ligue 1", "FL1", "LEAGUE",
                new AreaApiDto("France"),
                new CurrentSeasonApiDto(100, 10, DateTime.UtcNow, DateTime.UtcNow.AddMonths(9)),
                "ligue1.png"));

        var service = new FootballSyncService(Context, _footballDataService, _jobDispatcher, settings, _logger);

        // Act - First sync
        await service.SyncCompetitionsAsync();

        // Change API response
        _footballDataService.GetCompetitionAsync(2015, Arg.Any<CancellationToken>()).Returns(
            new CompetitionApiDto(2015, "Ligue 1 Updated", "FL1", "LEAGUE",
                new AreaApiDto("France"),
                new CurrentSeasonApiDto(100, 20, DateTime.UtcNow, DateTime.UtcNow.AddMonths(9)),
                "ligue1-new.png"));

        // Act - Second sync
        await service.SyncCompetitionsAsync();

        // Assert - should still have only one competition, but updated
        var competitions = Context.Competitions.Where(c => c.ExternalId == 2015).ToList();
        await Assert.That(competitions.Count).IsEqualTo(1);
        await Assert.That(competitions[0].Name).IsEqualTo("Ligue 1 Updated");
        await Assert.That(competitions[0].CurrentMatchday).IsEqualTo(20);
    }
}
