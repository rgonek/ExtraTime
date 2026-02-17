using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Domain.Enums;
using ExtraTime.Infrastructure.Configuration;
using ExtraTime.Infrastructure.Services.ExternalData;
using ExtraTime.UnitTests.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace ExtraTime.UnitTests.Infrastructure.Background;

[TestCategory(TestCategories.Significant)]
public sealed class OddsSyncBackgroundServiceTests
{
    [Test]
    public async Task GetNextRunUtc_BeforeMondayRun_ShouldReturnUpcomingMonday()
    {
        // Arrange
        var now = new DateTime(2026, 2, 15, 22, 0, 0, DateTimeKind.Utc); // Sunday

        // Act
        var nextRun = OddsSyncBackgroundService.GetNextRunUtc(now, 5);

        // Assert
        await Assert.That(nextRun).IsEqualTo(new DateTime(2026, 2, 16, 5, 0, 0, DateTimeKind.Utc));
    }

    [Test]
    public async Task GetNextRunUtc_AfterMondayRun_ShouldReturnNextWeekMonday()
    {
        // Arrange
        var now = new DateTime(2026, 2, 16, 10, 0, 0, DateTimeKind.Utc); // Monday after run hour

        // Act
        var nextRun = OddsSyncBackgroundService.GetNextRunUtc(now, 5);

        // Assert
        await Assert.That(nextRun).IsEqualTo(new DateTime(2026, 2, 23, 5, 0, 0, DateTimeKind.Utc));
    }

    [Test]
    public async Task StartAsync_ShouldRunInitialSyncAndRecordSuccess()
    {
        // Arrange
        var oddsDataService = Substitute.For<IOddsDataService>();
        oddsDataService.ImportAllLeaguesAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var integrationHealthService = Substitute.For<IIntegrationHealthService>();
        integrationHealthService.RecordSuccessAsync(
                Arg.Any<IntegrationType>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var service = CreateService(oddsDataService, integrationHealthService);

        // Act
        await service.StartAsync(CancellationToken.None);
        await Task.Delay(150);
        await service.StopAsync(CancellationToken.None);

        // Assert
        await oddsDataService.Received(1).ImportAllLeaguesAsync(Arg.Any<CancellationToken>());
        await integrationHealthService.Received(1).RecordSuccessAsync(
            IntegrationType.FootballDataUk,
            Arg.Any<TimeSpan>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task StartAsync_WhenSyncFails_ShouldRecordFailure()
    {
        // Arrange
        var oddsDataService = Substitute.For<IOddsDataService>();
        oddsDataService.ImportAllLeaguesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("boom")));

        var integrationHealthService = Substitute.For<IIntegrationHealthService>();
        integrationHealthService.RecordFailureAsync(
                Arg.Any<IntegrationType>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var service = CreateService(oddsDataService, integrationHealthService);

        // Act
        await service.StartAsync(CancellationToken.None);
        await Task.Delay(150);
        await service.StopAsync(CancellationToken.None);

        // Assert
        await integrationHealthService.Received(1).RecordFailureAsync(
            IntegrationType.FootballDataUk,
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    private static OddsSyncBackgroundService CreateService(
        IOddsDataService oddsDataService,
        IIntegrationHealthService integrationHealthService)
    {
        var logger = Substitute.For<ILogger<OddsSyncBackgroundService>>();
        var services = new ServiceCollection();
        services.AddSingleton(oddsDataService);
        services.AddSingleton(integrationHealthService);

        var provider = services.BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        var options = Options.Create(new FootballDataUkSettings
        {
            Enabled = true,
            SyncHourUtc = 5
        });

        return new OddsSyncBackgroundService(scopeFactory, options, logger);
    }
}
