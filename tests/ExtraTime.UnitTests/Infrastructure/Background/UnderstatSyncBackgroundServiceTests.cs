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
public sealed class UnderstatSyncBackgroundServiceTests
{
    [Test]
    public async Task GetNextRunUtc_BeforeConfiguredHour_ShouldReturnSameDayRun()
    {
        // Arrange
        var now = new DateTime(2026, 2, 16, 1, 30, 0, DateTimeKind.Utc);

        // Act
        var nextRun = UnderstatSyncBackgroundService.GetNextRunUtc(now, 4);

        // Assert
        await Assert.That(nextRun).IsEqualTo(new DateTime(2026, 2, 16, 4, 0, 0, DateTimeKind.Utc));
    }

    [Test]
    public async Task GetNextRunUtc_AfterConfiguredHour_ShouldReturnNextDayRun()
    {
        // Arrange
        var now = new DateTime(2026, 2, 16, 10, 0, 0, DateTimeKind.Utc);

        // Act
        var nextRun = UnderstatSyncBackgroundService.GetNextRunUtc(now, 4);

        // Assert
        await Assert.That(nextRun).IsEqualTo(new DateTime(2026, 2, 17, 4, 0, 0, DateTimeKind.Utc));
    }

    [Test]
    public async Task StartAsync_ShouldRunInitialSyncAndRecordSuccess()
    {
        // Arrange
        var understatService = Substitute.For<IUnderstatService>();
        understatService.SyncAllLeaguesAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var integrationHealthService = Substitute.For<IIntegrationHealthService>();
        integrationHealthService.RecordSuccessAsync(
                Arg.Any<IntegrationType>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var service = CreateService(understatService, integrationHealthService);

        // Act
        await service.StartAsync(CancellationToken.None);
        await Task.Delay(150);
        await service.StopAsync(CancellationToken.None);

        // Assert
        await understatService.Received(1).SyncAllLeaguesAsync(Arg.Any<CancellationToken>());
        await integrationHealthService.Received(1).RecordSuccessAsync(
            IntegrationType.Understat,
            Arg.Any<TimeSpan>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task StartAsync_WhenSyncFails_ShouldRecordFailure()
    {
        // Arrange
        var understatService = Substitute.For<IUnderstatService>();
        understatService.SyncAllLeaguesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("boom")));

        var integrationHealthService = Substitute.For<IIntegrationHealthService>();
        integrationHealthService.RecordFailureAsync(
                Arg.Any<IntegrationType>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var service = CreateService(understatService, integrationHealthService);

        // Act
        await service.StartAsync(CancellationToken.None);
        await Task.Delay(150);
        await service.StopAsync(CancellationToken.None);

        // Assert
        await integrationHealthService.Received(1).RecordFailureAsync(
            IntegrationType.Understat,
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    private static UnderstatSyncBackgroundService CreateService(
        IUnderstatService understatService,
        IIntegrationHealthService integrationHealthService)
    {
        var logger = Substitute.For<ILogger<UnderstatSyncBackgroundService>>();
        var services = new ServiceCollection();
        services.AddSingleton(understatService);
        services.AddSingleton(integrationHealthService);

        var provider = services.BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        var options = Options.Create(new UnderstatSettings
        {
            Enabled = true,
            SyncHourUtc = 4
        });

        return new UnderstatSyncBackgroundService(scopeFactory, options, logger);
    }
}
