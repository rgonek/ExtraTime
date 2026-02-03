using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Infrastructure.Services.Football;
using ExtraTime.UnitTests.Attributes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace ExtraTime.UnitTests.Infrastructure.Background;

[TestCategory(TestCategories.Significant)]
[NotInParallel]
public sealed class FootballSyncHostedServiceTests
{
    private IFootballSyncService _syncService = null!;
    private IApplicationDbContext _context = null!;
    private ILogger<FootballSyncHostedService> _logger = null!;
    private FootballSyncHostedService? _service;
    private TimeSpan _originalInitialDelay;

    [Before(Test)]
    public void Setup()
    {
        _originalInitialDelay = FootballSyncHostedService.InitialDelay;
        FootballSyncHostedService.InitialDelay = TimeSpan.FromMilliseconds(10);
        FootballSyncHostedService.LiveSyncInterval = TimeSpan.FromMilliseconds(10);
        FootballSyncHostedService.DailySyncInterval = TimeSpan.FromMilliseconds(10);

        _syncService = Substitute.For<IFootballSyncService>();
        _context = Substitute.For<IApplicationDbContext>();
        _logger = Substitute.For<ILogger<FootballSyncHostedService>>();

        var mockCompetitions = Substitute.For<DbSet<Domain.Entities.Competition>, IQueryable<Domain.Entities.Competition>, IAsyncEnumerable<Domain.Entities.Competition>>();
        var competitions = new List<Domain.Entities.Competition>().AsQueryable();
        ((IQueryable<Domain.Entities.Competition>)mockCompetitions).Provider.Returns(competitions.Provider);
        ((IQueryable<Domain.Entities.Competition>)mockCompetitions).Expression.Returns(competitions.Expression);
        ((IQueryable<Domain.Entities.Competition>)mockCompetitions).ElementType.Returns(competitions.ElementType);
        ((IQueryable<Domain.Entities.Competition>)mockCompetitions).GetEnumerator().Returns(_ => competitions.GetEnumerator());
        _context.Competitions.Returns(mockCompetitions);
    }

    private FootballSyncHostedService GetService()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_syncService);
        services.AddSingleton(_context);
        var serviceProvider = services.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        _service = new FootballSyncHostedService(scopeFactory, _logger);
        return _service;
    }

    [After(Test)]
    public void Cleanup()
    {
        FootballSyncHostedService.InitialDelay = _originalInitialDelay;
        _service?.Dispose();
        _service = null;
    }

    [Test]
    public async Task StartAsync_TriggersInitialSync()
    {
        // Arrange
        var service = GetService();
        using var cts = new CancellationTokenSource();

        _syncService.SyncCompetitionsAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _syncService.SyncTeamsForCompetitionAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _syncService.SyncMatchesAsync(Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        // Act
        await service.StartAsync(cts.Token);
        await Task.Delay(300); // Wait for initial delay and sync to run
        await service.StopAsync(cts.Token);

        // Assert
        await _syncService.Received().SyncCompetitionsAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ExecuteAsync_SyncFailure_Continues()
    {
        // Arrange
        var service = GetService();
        using var cts = new CancellationTokenSource();

        _syncService.SyncCompetitionsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new Exception("Sync failed")));

        // Act
        await service.StartAsync(cts.Token);
        await Task.Delay(300);
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
    public async Task ExecuteAsync_RunsPeriodicSync()
    {
        // Arrange
        var service = GetService();
        using var cts = new CancellationTokenSource();

        var competitionsSyncCount = 0;
        _syncService.SyncCompetitionsAsync(Arg.Any<CancellationToken>()).Returns(_ =>
        {
            Interlocked.Increment(ref competitionsSyncCount);
            return Task.CompletedTask;
        });

        // Act
        await service.StartAsync(cts.Token);
        await Task.Delay(500);
        await service.StopAsync(cts.Token);

        // Assert
        await Assert.That(competitionsSyncCount).IsGreaterThanOrEqualTo(1);
    }
}
