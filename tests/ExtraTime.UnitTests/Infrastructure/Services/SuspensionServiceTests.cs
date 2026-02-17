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
public sealed class SuspensionServiceTests
{
    [Test]
    public async Task SyncSuspensionsForUpcomingMatchesAsync_ShouldPersistSuspensionData()
    {
        // Arrange
        await using var context = CreateContext();
        var (homeTeam, _) = await SeedUpcomingMatchAsync(context);
        context.PlayerInjuries.Add(new PlayerInjury
        {
            TeamId = homeTeam.Id,
            ExternalPlayerId = 7,
            PlayerName = "Bukayo Saka",
            Position = "FWD",
            IsKeyPlayer = true,
            InjuryType = "Suspension - red card",
            InjurySeverity = "Minor",
            IsActive = true,
            LastUpdatedAt = DateTime.UtcNow
        });
        context.PlayerInjuries.Add(new PlayerInjury
        {
            TeamId = homeTeam.Id,
            ExternalPlayerId = 8,
            PlayerName = "Martin Odegaard",
            Position = "MID",
            IsKeyPlayer = true,
            InjuryType = "Hamstring strain",
            InjurySeverity = "Moderate",
            IsActive = true,
            LastUpdatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var injuryService = Substitute.For<IInjuryService>();
        injuryService.SyncInjuriesForUpcomingMatchesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        var integrationHealth = Substitute.For<IIntegrationHealthService>();
        var logger = Substitute.For<ILogger<SuspensionService>>();

        var service = new SuspensionService(context, injuryService, integrationHealth, logger);

        // Act
        await service.SyncSuspensionsForUpcomingMatchesAsync(3);

        // Assert
        var suspension = await context.TeamSuspensions.SingleAsync(x => x.TeamId == homeTeam.Id);
        await Assert.That(suspension.TotalSuspended).IsEqualTo(1);
        await Assert.That(suspension.KeyPlayersSuspended).IsEqualTo(1);
        await Assert.That(suspension.SuspensionImpactScore).IsGreaterThan(0d);
        await Assert.That(context.PlayerSuspensions.Count()).IsEqualTo(1);
        await integrationHealth.Received(1).RecordSuccessAsync(
            IntegrationType.SuspensionProvider,
            Arg.Any<TimeSpan>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetTeamSuspensionsAsOfAsync_WhenNoDataAsOfDate_ShouldReturnNull()
    {
        // Arrange
        await using var context = CreateContext();
        var team = Team.Create(10, "Arsenal", "Arsenal");
        context.Teams.Add(team);
        context.TeamSuspensions.Add(new TeamSuspensions
        {
            TeamId = team.Id,
            LastSyncedAt = new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc)
        });
        await context.SaveChangesAsync();

        var service = new SuspensionService(
            context,
            Substitute.For<IInjuryService>(),
            Substitute.For<IIntegrationHealthService>(),
            Substitute.For<ILogger<SuspensionService>>());

        // Act
        var result = await service.GetTeamSuspensionsAsOfAsync(
            team.Id,
            new DateTime(2026, 2, 9, 0, 0, 0, DateTimeKind.Utc));

        // Assert
        await Assert.That(result).IsNull();
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static async Task<(Team HomeTeam, Team AwayTeam)> SeedUpcomingMatchAsync(ApplicationDbContext context)
    {
        var competition = Competition.Create(2021, "Premier League", "PL", "England", type: CompetitionType.League);
        var home = Team.Create(10, "Arsenal", "Arsenal");
        var away = Team.Create(20, "Chelsea", "Chelsea");
        var match = Match.Create(
            externalId: 1001,
            competitionId: competition.Id,
            homeTeamId: home.Id,
            awayTeamId: away.Id,
            matchDateUtc: DateTime.UtcNow.AddDays(1),
            status: MatchStatus.Scheduled);

        context.Competitions.Add(competition);
        context.Teams.AddRange(home, away);
        context.Matches.Add(match);
        await context.SaveChangesAsync();
        return (home, away);
    }
}
