using ExtraTime.Application.Common.Interfaces;
using ExtraTime.Domain.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ExtraTime.Infrastructure.Services.Football;

public sealed class FootballSyncHostedService(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<FootballSyncHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan InitialDelay = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan DailySyncInterval = TimeSpan.FromHours(24);
    private static readonly TimeSpan LiveSyncInterval = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Football sync hosted service starting");

        await Task.Delay(InitialDelay, stoppingToken);

        await RunInitialSyncAsync(stoppingToken);

        using var dailyTimer = new PeriodicTimer(DailySyncInterval);
        using var liveTimer = new PeriodicTimer(LiveSyncInterval);

        var dailyTask = RunDailySyncLoopAsync(dailyTimer, stoppingToken);
        var liveTask = RunLiveSyncLoopAsync(liveTimer, stoppingToken);

        await Task.WhenAll(dailyTask, liveTask);
    }

    private async Task RunInitialSyncAsync(CancellationToken ct)
    {
        try
        {
            logger.LogInformation("Running initial football data sync");

            using var scope = serviceScopeFactory.CreateScope();
            var syncService = scope.ServiceProvider.GetRequiredService<IFootballSyncService>();
            var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

            await syncService.SyncCompetitionsAsync(ct);

            var competitions = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
                .ToListAsync(context.Competitions, ct);

            foreach (var competition in competitions)
            {
                await syncService.SyncTeamsForCompetitionAsync(competition.Id, ct);
            }

            await syncService.SyncMatchesAsync(ct: ct);

            logger.LogInformation("Initial football data sync completed");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during initial football data sync");
        }
    }

    private async Task RunDailySyncLoopAsync(PeriodicTimer timer, CancellationToken ct)
    {
        while (await timer.WaitForNextTickAsync(ct))
        {
            try
            {
                logger.LogInformation("Running daily football data sync");

                using var scope = serviceScopeFactory.CreateScope();
                var syncService = scope.ServiceProvider.GetRequiredService<IFootballSyncService>();

                await syncService.SyncMatchesAsync(ct: ct);

                logger.LogInformation("Daily football data sync completed");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during daily football data sync");
            }
        }
    }

    private async Task RunLiveSyncLoopAsync(PeriodicTimer timer, CancellationToken ct)
    {
        while (await timer.WaitForNextTickAsync(ct))
        {
            if (!IsMatchHours())
            {
                continue;
            }

            try
            {
                logger.LogDebug("Running live match sync");

                using var scope = serviceScopeFactory.CreateScope();
                var syncService = scope.ServiceProvider.GetRequiredService<IFootballSyncService>();

                await syncService.SyncLiveMatchResultsAsync(ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during live match sync");
            }
        }
    }

    private static bool IsMatchHours()
    {
        var utcHour = Clock.UtcNow.Hour;
        return utcHour is >= 10 and <= 23;
    }
}
