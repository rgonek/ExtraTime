using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Infrastructure.Configuration;
using ExtraTime.Infrastructure.Services.ExternalData;
using ExtraTime.UnitTests.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace ExtraTime.UnitTests.Infrastructure.Background;

[TestCategory(TestCategories.Significant)]
public sealed class EloSyncBackgroundServiceTests
{
    [Test]
    public async Task GetNextRunUtc_BeforeConfiguredHour_ShouldReturnSameDayRun()
    {
        // Arrange
        var now = new DateTime(2026, 2, 16, 1, 30, 0, DateTimeKind.Utc);

        // Act
        var nextRun = EloSyncBackgroundService.GetNextRunUtc(now, 3);

        // Assert
        await Assert.That(nextRun).IsEqualTo(new DateTime(2026, 2, 16, 3, 0, 0, DateTimeKind.Utc));
    }

    [Test]
    public async Task GetNextRunUtc_AfterConfiguredHour_ShouldReturnNextDayRun()
    {
        // Arrange
        var now = new DateTime(2026, 2, 16, 10, 0, 0, DateTimeKind.Utc);

        // Act
        var nextRun = EloSyncBackgroundService.GetNextRunUtc(now, 3);

        // Assert
        await Assert.That(nextRun).IsEqualTo(new DateTime(2026, 2, 17, 3, 0, 0, DateTimeKind.Utc));
    }

    [Test]
    public async Task StartAsync_ShouldRunInitialSync()
    {
        // Arrange
        var eloRatingService = Substitute.For<IEloRatingService>();
        eloRatingService.SyncEloRatingsAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var service = CreateService(eloRatingService);

        // Act
        await service.StartAsync(CancellationToken.None);
        await Task.Delay(150);
        await service.StopAsync(CancellationToken.None);

        // Assert
        await eloRatingService.Received(1).SyncEloRatingsAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task StartAsync_WhenSyncFails_ShouldStillAttemptInitialSync()
    {
        // Arrange
        var eloRatingService = Substitute.For<IEloRatingService>();
        eloRatingService.SyncEloRatingsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("boom")));
        var service = CreateService(eloRatingService);

        // Act
        await service.StartAsync(CancellationToken.None);
        await Task.Delay(150);
        await service.StopAsync(CancellationToken.None);

        // Assert
        await eloRatingService.Received(1).SyncEloRatingsAsync(Arg.Any<CancellationToken>());
    }

    private static EloSyncBackgroundService CreateService(IEloRatingService eloRatingService)
    {
        var logger = Substitute.For<ILogger<EloSyncBackgroundService>>();
        var services = new ServiceCollection();
        services.AddSingleton(eloRatingService);

        var provider = services.BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        var options = Options.Create(new ClubEloSettings
        {
            Enabled = true,
            SyncHourUtc = 3
        });

        return new EloSyncBackgroundService(scopeFactory, options, logger);
    }
}
