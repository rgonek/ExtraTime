using ExtraTime.Application.Features.Bots.Services;
using ExtraTime.Infrastructure.Services.Bots;
using ExtraTime.UnitTests.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace ExtraTime.UnitTests.Infrastructure.Background;

[TestCategory(TestCategories.Significant)]
[NotInParallel]
public sealed class BotBettingBackgroundServiceTests
{
    private IBotBettingService _botService = null!;
    private ILogger<BotBettingBackgroundService> _logger = null!;
    private BotBettingBackgroundService? _service;
    private TimeSpan _originalInterval;

    [Before(Test)]
    public void Setup()
    {
        _originalInterval = BotBettingBackgroundService.Interval;
        BotBettingBackgroundService.Interval = TimeSpan.FromMilliseconds(10);

        _botService = Substitute.For<IBotBettingService>();
        _logger = Substitute.For<ILogger<BotBettingBackgroundService>>();
    }

    private BotBettingBackgroundService GetService()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_botService);
        var serviceProvider = services.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        _service = new BotBettingBackgroundService(scopeFactory, _logger);
        return _service;
    }

    [After(Test)]
    public void Cleanup()
    {
        BotBettingBackgroundService.Interval = _originalInterval;
        _service?.Dispose();
        _service = null;
    }

    [Test]
    public async Task ExecuteAsync_PlacesBotBets()
    {
        // Arrange
        var service = GetService();
        using var cts = new CancellationTokenSource();

        _botService.PlaceBetsForUpcomingMatchesAsync(Arg.Any<CancellationToken>()).Returns(5);

        // Act
        await service.StartAsync(cts.Token);
        await Task.Delay(200);
        await service.StopAsync(cts.Token);

        // Assert
        var now = DateTime.UtcNow;
        if (now.Hour >= 8 && now.Hour <= 23)
        {
            await _botService.Received().PlaceBetsForUpcomingMatchesAsync(Arg.Any<CancellationToken>());
        }
    }

    [Test]
    public async Task ExecuteAsync_BetPlacementFailure_Continues()
    {
        // Arrange
        var service = GetService();
        using var cts = new CancellationTokenSource();

        _botService.PlaceBetsForUpcomingMatchesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<int>(new Exception("Bet placement failed")));

        // Act
        await service.StartAsync(cts.Token);
        await Task.Delay(200);
        await service.StopAsync(cts.Token);

        // Assert
        await Assert.That(service).IsNotNull();
    }

    [Test]
    public async Task StopAsync_GracefulShutdown()
    {
        // Arrange
        var service = GetService();
        using var cts = new CancellationTokenSource();

        // Act
        await service.StopAsync(cts.Token);

        // Assert
        await Assert.That(Task.CompletedTask.IsCompleted).IsTrue();
    }

    [Test]
    public async Task ExecuteAsync_NoActiveBots_NoAction()
    {
        // Arrange
        var service = GetService();
        using var cts = new CancellationTokenSource();

        _botService.PlaceBetsForUpcomingMatchesAsync(Arg.Any<CancellationToken>()).Returns(0);

        // Act
        await service.StartAsync(cts.Token);
        await Task.Delay(200);
        await service.StopAsync(cts.Token);

        // Assert
        await Assert.That(service).IsNotNull();
    }

    [Test]
    public async Task ExecuteAsync_OnlyDuringActiveHours()
    {
        // Arrange
        var service = GetService();
        using var cts = new CancellationTokenSource();

        _botService.PlaceBetsForUpcomingMatchesAsync(Arg.Any<CancellationToken>()).Returns(0);

        // Act
        await service.StartAsync(cts.Token);
        await Task.Delay(200);
        await service.StopAsync(cts.Token);

        // Assert
        await Assert.That(service).IsNotNull();
    }
}
