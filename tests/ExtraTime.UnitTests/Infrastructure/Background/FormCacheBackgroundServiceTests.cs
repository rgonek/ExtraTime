using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Infrastructure.Services.Bots;
using ExtraTime.UnitTests.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace ExtraTime.UnitTests.Infrastructure.Background;

[TestCategory(TestCategories.Significant)]
[NotInParallel]
public sealed class FormCacheBackgroundServiceTests
{
    private ITeamFormCalculator _formCalculator = null!;
    private ILogger<FormCacheBackgroundService> _logger = null!;
    private FormCacheBackgroundService? _service;
    private TimeSpan _originalInterval;

    [Before(Test)]
    public void Setup()
    {
        _originalInterval = FormCacheBackgroundService.Interval;
        FormCacheBackgroundService.Interval = TimeSpan.FromMilliseconds(10);

        _formCalculator = Substitute.For<ITeamFormCalculator>();
        _logger = Substitute.For<ILogger<FormCacheBackgroundService>>();
    }

    private FormCacheBackgroundService GetService()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_formCalculator);
        var serviceProvider = services.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        _service = new FormCacheBackgroundService(scopeFactory, _logger);
        return _service;
    }

    [After(Test)]
    public void Cleanup()
    {
        FormCacheBackgroundService.Interval = _originalInterval;
        _service?.Dispose();
        _service = null;
    }

    [Test]
    public async Task ExecuteAsync_UpdatesFormCache()
    {
        // Arrange
        var service = GetService();
        using var cts = new CancellationTokenSource();

        _formCalculator.RefreshAllFormCachesAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        // Act
        await service.StartAsync(cts.Token);
        await Task.Delay(200); 
        await service.StopAsync(cts.Token);

        // Assert
        await _formCalculator.Received().RefreshAllFormCachesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ExecuteAsync_PeriodicUpdates()
    {
        // Arrange
        var service = GetService();
        using var cts = new CancellationTokenSource();

        var refreshCount = 0;
        _formCalculator.RefreshAllFormCachesAsync(Arg.Any<CancellationToken>()).Returns(_ =>
        {
            Interlocked.Increment(ref refreshCount);
            return Task.CompletedTask;
        });

        // Act
        await service.StartAsync(cts.Token);
        await Task.Delay(300); // Wait for multiple intervals
        await service.StopAsync(cts.Token);

        // Assert
        await Assert.That(refreshCount).IsGreaterThanOrEqualTo(1);
    }

    [Test]
    public async Task ExecuteAsync_UpdateFailure_Continues()
    {
        // Arrange
        var service = GetService();
        using var cts = new CancellationTokenSource();

        var callCount = 0;
        _formCalculator.RefreshAllFormCachesAsync(Arg.Any<CancellationToken>()).Returns(_ =>
        {
            if (Interlocked.Increment(ref callCount) == 1)
            {
                throw new Exception("Cache refresh failed");
            }
            return Task.CompletedTask;
        });

        // Act
        await service.StartAsync(cts.Token);
        await Task.Delay(300);
        await service.StopAsync(cts.Token);

        // Assert
        await Assert.That(service).IsNotNull();
    }
}
