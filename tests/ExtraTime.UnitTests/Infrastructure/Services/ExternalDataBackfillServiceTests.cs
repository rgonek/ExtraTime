using System.Text.Json;
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
public sealed class ExternalDataBackfillServiceTests
{
    [Test]
    public async Task BackfillForLeagueAsync_WhenInterrupted_ResumesFromLastCompletedSeason()
    {
        // Arrange
        await using var context = CreateContext();
        await SeedLeagueAsync(context, "PL", 2022, 2024);

        var understatService = Substitute.For<IUnderstatService>();
        var oddsDataService = Substitute.For<IOddsDataService>();
        var eloRatingService = Substitute.For<IEloRatingService>();
        var injuryService = Substitute.For<IInjuryService>();
        var logger = Substitute.For<ILogger<ExternalDataBackfillService>>();

        var failOn2023Once = true;
        understatService
            .SyncLeagueXgStatsAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var season = call.ArgAt<string>(1);
                if (season == "2023" && failOn2023Once)
                {
                    failOn2023Once = false;
                    throw new InvalidOperationException("Simulated interruption");
                }

                return Task.FromResult(new List<TeamXgStats>());
            });

        oddsDataService.ImportSeasonOddsAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<DateTime?>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        injuryService.SyncInjuriesForUpcomingMatchesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var service = new ExternalDataBackfillService(
            context,
            understatService,
            oddsDataService,
            eloRatingService,
            injuryService,
            logger);

        // Act
        var firstRun = () => service.BackfillForLeagueAsync("PL", 2022, 2024);

        // Assert
        await Assert.That(firstRun).Throws<InvalidOperationException>();
        var checkpointAfterFailure = await context.IntegrationStatuses
            .AsNoTracking()
            .SingleAsync(x => x.IntegrationName == "Backfill:Understat:PL");
        var failedPayload = JsonSerializer.Deserialize<CheckpointPayload>(checkpointAfterFailure.LastErrorDetails!);
        await Assert.That(failedPayload?.LastCompletedSeason).IsEqualTo(2022);

        await service.BackfillForLeagueAsync("PL", 2022, 2024);

        await understatService.Received(1).SyncLeagueXgStatsAsync(
            "PL",
            "2022",
            Arg.Any<DateTime?>(),
            Arg.Any<CancellationToken>());
        await understatService.Received(2).SyncLeagueXgStatsAsync(
            "PL",
            "2023",
            Arg.Any<DateTime?>(),
            Arg.Any<CancellationToken>());
        await understatService.Received(1).SyncLeagueXgStatsAsync(
            "PL",
            "2024",
            Arg.Any<DateTime?>(),
            Arg.Any<CancellationToken>());

        var checkpointAfterResume = await context.IntegrationStatuses
            .AsNoTracking()
            .SingleAsync(x => x.IntegrationName == "Backfill:Understat:PL");
        var resumedPayload = JsonSerializer.Deserialize<CheckpointPayload>(checkpointAfterResume.LastErrorDetails!);
        await Assert.That(resumedPayload?.LastCompletedSeason).IsEqualTo(2024);
    }

    private static async Task SeedLeagueAsync(
        ApplicationDbContext context,
        string leagueCode,
        int fromSeason,
        int toSeason)
    {
        var competition = Competition.Create(99, "Premier League", leagueCode, "England", type: CompetitionType.League);
        var team = Team.Create(101, "Arsenal", "Arsenal");

        context.Competitions.Add(competition);
        context.Teams.Add(team);

        for (var season = fromSeason; season <= toSeason; season++)
        {
            context.CompetitionTeams.Add(new CompetitionTeam
            {
                CompetitionId = competition.Id,
                TeamId = team.Id,
                Season = season
            });
        }

        await context.SaveChangesAsync();
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private sealed record CheckpointPayload
    {
        public int? LastCompletedSeason { get; init; }
    }
}
